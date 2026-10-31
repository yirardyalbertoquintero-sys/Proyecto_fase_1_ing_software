"""Genera tres SVG y un draw.io nativo desde el subconjunto Mermaid de CU-10.

Python 3, sin dependencias. No es un conversor Mermaid general: reconoce las
declaraciones y relaciones empleadas en estas tres fuentes y falla ante otras.
La distribución es deliberada; los nombres, miembros y relaciones se leen del MMD.
No importa ni ejecuta Generar-Diagramas.py ni modifica sus figuras anteriores.
"""
from pathlib import Path
from html import escape
import re
import textwrap
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "docs/fase-3/diagramas"
WIDTH, CARD, GAP, LEFT = 2340, 560, 120, 60
SHIFT_X = 240  # Margen para las asociaciones largas a la izquierda del historial.
FONT, LINE = 16, 23

PAGES = [
    ("aplicacion", "CU-10 · Aplicación y persistencia", [
        ["VistaPrototipoCU10", "FronteraHttpCU10", "RevisionCotizacionAdminService"],
        ["PedidoAdministracionRepository", "IPedidoAdministracionRepository", "ReglasAdministrativasPedido"],
        ["ImpactoTextilDbContext", "ResultadoOperacionAdmin", "TimeProvider"],
    ]),
    ("dtos", "CU-10 · DTO existentes (miembros seleccionados)", [
        ["FiltroPedidosAdminDto", "ProponerCotizacionDto", "LineaCotizacionPropuestaDto"],
        ["DecidirCotizacionDto", "DecidirVersionDisenoDto", "RegistrarVersionDisenoTextoDto"],
        ["ArchivarPedidoDto", "ResultadoCotizacionAdminDto", "ResultadoVersionDisenoAdminDto"],
        ["ResultadoArchivoPedidoDto", "ResumenPedidoAdminDto", "ExpedientePedidoAdminDto"],
    ]),
    ("historial", "CU-10 · Entidades, historial y proyección", [
        ["Pedido", "DetallePedido", "VersionDiseno"],
        ["VersionCotizacion", "DetalleCotizacion", "Usuario"],
        ["Pago", "vPedido"],
    ]),
]


def csharp(value):
    """Sustituye delimitadores genéricos; conserva la visibilidad interna ~."""
    result = []
    for i, char in enumerate(value):
        if char == "~" and i:
            result.append("<" if i + 1 < len(value) and value[i + 1].isalpha() else ">")
        else:
            result.append(char)
    if value.endswith("$"):
        return "".join(result)[:-1] + " [static]"
    return "".join(result)


def parse(path):
    nodes, edges, current = {}, [], None
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line or line.startswith("%%") or line in ("classDiagram", "direction TB"):
            continue
        match = re.fullmatch(r'class (\w+)(~\w+~)?(?:\["([^"]+)"\])? \{', line)
        if match:
            key, generic, label = match.groups()
            if key in nodes:
                raise ValueError(f"Clase repetida: {key}")
            current = dict(key=key, title=label or csharp(key + (generic or "")), members=[], stereotype="")
            nodes[key] = current
        elif line == "}":
            current = None
        elif current is not None:
            if line.startswith("<<") and line.endswith(">>"):
                current["stereotype"] = line[2:-2]
            else:
                current["members"].append(csharp(line))
        else:
            match = re.fullmatch(r'(\w+)(?: "([^"]+)")? (\*--|-->|\.\.>|\.\.\|>)(?: "([^"]+)")? (\w+) : (.+)', line)
            if not match:
                raise ValueError(f"Sintaxis no admitida en {path.name}: {line}")
            source, source_count, kind, target_count, target, label = match.groups()
            edges.append(dict(source=source, target=target, kind=kind, label=label,
                              source_count=source_count, target_count=target_count))
    for edge in edges:
        if edge["source"] not in nodes or edge["target"] not in nodes:
            raise ValueError(f"Relación sin clase: {edge}")
    for node in nodes.values():
        node["lines"] = [part for member in node["members"]
                         for part in textwrap.wrap(member, width=55, subsequent_indent="  ",
                                                   break_long_words=True, break_on_hyphens=False)]
        node["w"], node["h"] = CARD, 82 + LINE * len(node["lines"])
    return nodes, edges


def port(node, side, fraction=.5):
    x, y, w, h = (node[k] for k in ("x", "y", "w", "h"))
    return {"L": (x, y + h*fraction), "R": (x+w, y+h*fraction),
            "T": (x+w*fraction, y), "B": (x+w*fraction, y+h)}[side]


def route(sheet, edge, nodes, rows):
    s, t = nodes[edge["source"]], nodes[edge["target"]]
    label = edge["label"]
    # Each route remains outside the class boxes. Parallel domain associations
    # use different anchors and the vertical corridors between columns.
    if sheet == "aplicacion":
        specs = {
            "GET de sesion existente": ("B", "T", .3, .5, []),
            "conexion pendiente": ("R", "L", .25, .25, []),
            "orquestacion pendiente": ("R", "L", .6, .2, []),
            "inyeccion": ("L", "T", .8, .8, [(1370, s["y"]+s["h"]*.8), (1370, t["y"]-65), (t["x"]+CARD*.8, t["y"]-65)]),
            "implementa": ("R", "L", .3, .3, []),
            "consulta y persistencia": ("B", "T", .5, .5, []),
            "reglas del SRS": ("B", "T", .5, .5, []),
            "devuelve resultado": ("L", "R", .94, .6, [(1345, s["y"]+s["h"]*.94), (1345, t["y"]+t["h"]*.6)]),
            "reloj inyectado": ("R", "R", .45, .5, [(2035, s["y"]+s["h"]*.45), (2035, t["y"]+t["h"]*.5)]),
        }
        a, b, af, bf, middle = specs[label]
    elif sheet == "dtos":
        a, b, af, bf, middle = "R", "L", .5, .5, []
        if label == "cabecera":
            a, b = "L", "R"
    else:
        lane0 = rows[1]-85
        lane1 = rows[2]-80
        specs = {
            ("Pedido", "detalles"): ("R", "L", .25, .25, []),
            ("Pedido", "historial"): ("B", "T", .25, .25, []),
            ("DetallePedido", "historial"): ("R", "L", .25, .25, []),
            ("VersionCotizacion", "importes"): ("R", "L", .3, .3, []),
            ("DetalleCotizacion", "cantidad original"): ("T", "B", .3, .3, []),
            ("Pedido", "vigente aceptada"): ("B", "T", .7, .7, []),
            ("DetallePedido", "vigente aprobada"): ("R", "L", .7, .7, []),
            ("DetalleCotizacion", "arte cotizado"): ("R", "B", .18, .25, [(1360, s["y"]+s["h"]*.18), (1360, lane0), (t["x"]+CARD*.25, lane0)]),
            ("Usuario", "registra"): ("T", "B", .72, .72, []) if t["key"] == "VersionDiseno" else
                ("B", "B", .5, .75, [(s["x"]+CARD*.5, lane1), (t["x"]+CARD*.75, lane1)]),
            ("Pedido", "consulta verificados"): ("L", "L", .7, .5, [(20, s["y"]+s["h"]*.7), (20, t["y"]+t["h"]*.5)]),
            ("vPedido", "proyeccion"): ("L", "L", .75, .3, [(660, s["y"]+s["h"]*.75), (660, lane1+35), (5, lane1+35), (5, t["y"]+t["h"]*.3)]),
            ("vPedido", "suma verificados"): ("L", "R", .25, .25, []),
        }
        a, b, af, bf, middle = specs[(s["key"], label)]
    return [port(s, a, af), *middle, port(t, b, bf)]


def label_position(points):
    segments = [(abs(b[0]-a[0])+abs(b[1]-a[1]), a, b) for a, b in zip(points, points[1:])]
    _, a, b = max(segments, key=lambda item: item[0])
    return (a[0]+b[0])/2, (a[1]+b[1])/2


def generate():
    mxfile = ET.Element("mxfile", host="app.diagrams.net", type="device")
    counts = []
    for sheet, title, grid in PAGES:
        basename = "clases-cu10-"+sheet
        nodes, edges = parse(OUT/(basename+".mmd"))
        used = {key for row in grid for key in row}
        if sheet == "aplicacion":
            used.add("SesionController")
        if used != nodes.keys():
            raise ValueError(f"Distribución incompleta en {sheet}: {used ^ nodes.keys()}")
        row_y, y = [], 140
        for row in grid:
            row_y.append(y)
            for col, key in enumerate(row):
                nodes[key].update(x=LEFT+col*(CARD+GAP), y=y)
            row_h = max(nodes[key]["h"] for key in row)
            if sheet == "aplicacion" and len(row_y) == 1:
                session = nodes["SesionController"]
                session.update(x=LEFT, y=y+nodes["VistaPrototipoCU10"]["h"]+120)
                row_h = max(row_h, session["y"]+session["h"]-y)
            y += row_h+180
        height = y-70
        diagram = ET.SubElement(mxfile, "diagram", id=basename, name=title)
        model = ET.SubElement(diagram, "mxGraphModel", page="1", pageWidth=str(WIDTH),
                              pageHeight=str(height), grid="1", gridSize="10")
        root = ET.SubElement(model, "root")
        ET.SubElement(root, "mxCell", id="0")
        ET.SubElement(root, "mxCell", id="1", parent="0")
        svg = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{WIDTH}" height="{height}" viewBox="0 0 {WIDTH} {height}">',
               '<defs><marker id="arrow" markerWidth="12" markerHeight="12" refX="10" refY="6" orient="auto-start-reverse"><path d="M1 1 L10 6 L1 11" fill="none" stroke="#42556b"/></marker>',
               '<marker id="triangle" markerWidth="14" markerHeight="14" refX="12" refY="7" orient="auto"><path d="M1 1 L12 7 L1 13 Z" fill="white" stroke="#42556b"/></marker>',
               '<marker id="diamond" markerWidth="16" markerHeight="12" refX="1" refY="6" orient="auto"><path d="M1 6 L8 1 L15 6 L8 11 Z" fill="#42556b"/></marker></defs>',
               f'<rect width="{WIDTH}" height="{height}" fill="white"/>']

        def text(key, value, x, y, w, h, size=16, bold=False, center=False):
            cell = ET.SubElement(root, "mxCell", id=key, value=escape(value), vertex="1", parent="1",
                style=f'text;html=1;whiteSpace=wrap;align={"center" if center else "left"};verticalAlign=middle;fontSize={size};fontColor=#172B40;fontStyle={1 if bold else 0};fillColor=#FFFFFF;strokeColor=none;')
            ET.SubElement(cell, "mxGeometry", x=str(x), y=str(y), width=str(w), height=str(h), **{"as":"geometry"})
            lines = value.split("\n")
            for j, line in enumerate(lines):
                svg.append(f'<text x="{x+w/2 if center else x}" y="{y+size+3+j*(size+4)}" text-anchor="{"middle" if center else "start"}" font-family="Arial" font-size="{size}" font-weight="{"bold" if bold else "normal"}" fill="#172b40" stroke="white" stroke-width="5" paint-order="stroke">{escape(line)}</text>')

        text("title", title, 60, 20, WIDTH-120, 35, 28, True)
        text("subtitle", "Tipos y miembros del código al 30-10-2026. Controlador de CU-10 y conexión real pendientes.", 60, 66, WIDTH-120, 28)
        for index, edge in enumerate(edges):
            points = route(sheet, edge, nodes, row_y)
            s, t = nodes[edge["source"]], nodes[edge["target"]]
            kind = edge["kind"]
            dotted = kind.startswith("..")
            arrow = "triangle" if kind == "..|>" else "arrow"
            marker = ' marker-start="url(#diamond)"' if kind == "*--" else f' marker-end="url(#{arrow})"'
            dash = ' stroke-dasharray="7 5"' if dotted else ""
            svg.append(f'<polyline points="{" ".join(f"{x+SHIFT_X},{y}" for x,y in points)}" fill="none" stroke="#42556b" stroke-width="2"{marker}{dash}/>')
            start, end = points[0], points[-1]
            style = (f'exitX={(start[0]-s["x"])/CARD};exitY={(start[1]-s["y"])/s["h"]};exitPerimeter=0;'
                     f'entryX={(end[0]-t["x"])/CARD};entryY={(end[1]-t["y"])/t["h"]};entryPerimeter=0;'
                     'edgeStyle=segmentEdgeStyle;rounded=0;html=1;strokeColor=#42556B;'
                     f'dashed={int(dotted)};startArrow={"diamond" if kind == "*--" else "none"};'
                     f'endArrow={"none" if kind == "*--" else "block" if kind == "..|>" else "open"};endFill=0;')
            cell = ET.SubElement(root, "mxCell", id=f"edge{index}", value="", edge="1", parent="1",
                                 source=s["key"], target=t["key"], style=style)
            geom = ET.SubElement(cell, "mxGeometry", relative="1", **{"as":"geometry"})
            bends = ET.SubElement(geom, "Array", **{"as":"points"})
            for x, y in points[1:-1]:
                ET.SubElement(bends, "mxPoint", x=str(x+SHIFT_X), y=str(y))
            lx, ly = label_position(points)
            lx = 120 if lx < LEFT else lx+SHIFT_X
            label = edge["label"]
            if edge["source_count"]:
                label += f'\n{edge["source_count"]} → {edge["target_count"]}'
            # Narrow horizontal corridors need wrapped labels; vertical ones
            # get a small label beside the line rather than over another box.
            parts = textwrap.wrap(label.split("\n")[0], width=12 if lx > 200 else 24)
            if "\n" in label:
                parts.append(label.split("\n")[1])
            label_w = 116 if lx > 200 else 200
            label_h = len(parts)*20
            text(f"label{index}", "\n".join(parts), lx-label_w/2, ly-label_h/2-4, label_w, label_h, 15, center=True)
        for node in nodes.values():
            x,y,w,h = (node[k] for k in ("x","y","w","h"))
            x += SHIFT_X
            pending = node["stereotype"] in ("prototipo", "pendiente")
            color = "#845b1c" if pending else "#193c58"
            svg.extend([f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="#f7f9fc" stroke="{color}" stroke-width="1.5"/>',
                        f'<rect x="{x}" y="{y}" width="{w}" height="65" fill="{color}"/>',
                        f'<text x="{x+16}" y="{y+24}" font-family="Arial" font-size="18" font-weight="bold" fill="white">{escape(node["title"])}</text>',
                        f'<text x="{x+16}" y="{y+49}" font-family="Arial" font-size="15" fill="white">«{escape(node["stereotype"])}»</text>'])
            for i, line in enumerate(node["lines"]):
                svg.append(f'<text x="{x+14}" y="{y+86+i*LINE}" font-family="Consolas,monospace" font-size="{FONT}" fill="#172b40">{escape(line)}</text>')
            value = f'<b>{escape(node["title"])}</b><br>«{escape(node["stereotype"])}»<hr><div style="font-family:Consolas,monospace;font-size:16px;line-height:23px">'+"<br>".join(escape(line).replace(" ", "&nbsp;") for line in node["lines"])+"</div>"
            cell = ET.SubElement(root, "mxCell", id=node["key"], value=value, vertex="1", parent="1",
                style=f'rounded=0;whiteSpace=wrap;html=1;align=left;verticalAlign=top;spacing=12;fontSize=18;fillColor=#F7F9FC;strokeColor={color};')
            ET.SubElement(cell, "mxGeometry", x=str(x), y=str(y), width=str(w), height=str(h), **{"as":"geometry"})
            if x+w > WIDTH or y+h > height:
                raise ValueError(f"Clase fuera de página: {node['key']}")
        footer = "+ público · − privado · [static] estático · ? admite null · … parámetros abreviados. Ver 14-diseno-clases-cu10.md."
        text("footer", footer, 60, height-55, WIDTH-120, 32, 16)
        svg.append("</svg>")
        (OUT/(basename+".svg")).write_text("\n".join(svg)+"\n", encoding="utf-8")
        counts.append(f"{sheet}: {len(nodes)} cajas, {len(edges)} relaciones")
    ET.indent(mxfile, space="  ")
    ET.ElementTree(mxfile).write(OUT/"Clases_CU10.drawio", encoding="utf-8", xml_declaration=True)
    print("Generadas tres láminas SVG y tres páginas nativas editables en Clases_CU10.drawio.")
    print("; ".join(counts))


if __name__ == "__main__":
    generate()
