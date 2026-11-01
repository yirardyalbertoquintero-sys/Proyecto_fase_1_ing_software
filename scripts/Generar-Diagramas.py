"""Genera figuras vectoriales y páginas nativas editables en draw.io.
Python 3, biblioteca estándar. No modifica el documento fuente ni el esquema.
Los campos completos están en SQL; las páginas con contexto lo identifican.
"""
from pathlib import Path
from html import escape
from xml.etree.ElementTree import Element, SubElement, ElementTree

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs/fase-3/diagramas'


def node(key, title, x, y, fields, width=320):
    return dict(key=key, title=title, x=x, y=y, w=width, h=48+23*len(fields), fields=fields)


def edge(source, target, label, points, label_at):
    return dict(source=source, target=target, label=label, points=points, label_at=label_at)


pages = [
    dict(name='mer-principal', title='Modelo relacional 1 de 3', subtitle='Núcleo y entrega. PK: clave primaria; FK: referencia; UK: clave alternativa.', width=1560, height=1200,
    nodes=[
        node('u','Usuario',40,100,['PK id','UK correo','nombre; passwordHash','rol; activo','aceptacionTerminos','fechaAceptacion']),
        node('p','Pedido',600,100,['PK id; UK codigo','FK cliente','fechaOrden','fechaEntregaAcordada','fechaEntregaReal','estado; modalidadEntrega','direccionEntrega','FK versionCotizacionVigente','condicionAdministrativa','motivoArchivo; fechaArchivo','FK responsableArchivo','confirmacion; huellaSolicitud','controlConcurrencia']),
        node('d','DetallePedido',1160,100,['PK id','FK pedido; FK producto','cantidad; talla; color; tela','empaque; observaciones','ideaDiseno; archivoDisenoURL','precioBaseReferencia','FK versionDisenoVigente']),
        node('pr','Producto',40,740,['PK id','nombre; color; talla; tela','imagenURL; precioBase; activo']),
        node('g','Pago',600,740,['PK id; FK pedido','monto; fecha; medio','referencia; estadoVerificacion','comprobanteURL','fechaVerificacion','FK responsableVerificacion','confirmacion','controlConcurrencia']),
        node('c','ConstanciaEntrega',1160,740,['PK id; FK pedido','decision; fecha','FK responsable','referencia']),
    ],edges=[
        edge('u','p','cliente: 1 → 0..*',[(360,145),(600,145)],(480,130)),
        edge('u','p','archivo: 0..1 → 0..*',[(360,220),(600,220)],(480,205)),
        edge('u','g','revisor: 0..1 → 0..*',[(360,270),(440,270),(440,795),(600,795)],(480,570)),
        edge('u','c','responsable: 1 → 0..*',[(200,286),(200,640),(1100,640),(1100,830),(1160,830)],(820,625)),
        edge('p','d','1 → 1..* detalles',[(920,180),(1160,180)],(1040,165)),
        edge('pr','d','producto: 1 → 0..*',[(360,790),(390,790),(390,580),(1520,580),(1520,290),(1480,290)],(1310,565)),
        edge('p','g','1 → 0..* pagos',[(760,447),(760,740)],(850,525)),
        edge('p','c','1 → 0..* constancias',[(920,390),(1030,390),(1030,780),(1160,780)],(1030,470)),
    ]),
    dict(name='mer-versiones',title='Modelo relacional 2 de 3',subtitle='Historial y elección de versiones. Las cantidades originales se conservan por referencia inmutable.',width=1560,height=1120,
    nodes=[
        node('p','Pedido · contexto',40,100,['PK id','FK versionCotizacionVigente']),
        node('v','VersionCotizacion',600,100,['PK id; FK pedido','UK pedido + numeroVersion','fechaRegistro','FK responsableRegistro','decision; fechaDecision','FK responsableDecision','referenciaDecision']),
        node('dc','DetalleCotizacion',1160,100,['PK/FK versionCotizacion','PK/FK detalle','precioUnitarioAcordado','FK versionDiseno (opcional)']),
        node('u','Usuario · contexto',40,710,['PK id','rol']),
        node('vd','VersionDiseno',600,710,['PK id; FK detalle','UK detalle + numeroVersion','archivoURL; descripcion','fechaRegistro','FK responsableRegistro','decision; fechaDecision','FK responsableDecision','referenciaDecision']),
        node('d','DetallePedido · contexto',1160,710,['PK id; FK pedido','cantidad original inmutable','FK versionDisenoVigente']),
    ],edges=[
        edge('p','v','pedido: 1 → 0..*',[(360,140),(600,140)],(480,125)),
        edge('v','p','vigente: 0..1 ↔ 0..1',[(600,180),(360,180)],(480,208)),
        edge('v','dc','1 → 1..* importes',[(920,150),(1160,150)],(1040,135)),
        edge('dc','d','detalle: 0..* → 1',[(1320,240),(1320,710)],(1400,475)),
        edge('d','vd','detalle: 1 → 0..*',[(1160,755),(920,755)],(1040,740)),
        edge('vd','d','vigente: 0..1 ↔ 0..1',[(920,800),(1160,800)],(1040,825)),
        edge('vd','dc','arte: 0..1 → 0..*',[(920,880),(1070,880),(1070,210),(1160,210)],(1070,550)),
        edge('u','v','registro 1 / decisión 0..1 → 0..*',[(360,750),(450,750),(450,290),(600,290)],(455,495)),
        edge('u','vd','registro 1 / decisión 0..1 → 0..*',[(360,800),(600,800)],(480,850)),
    ]),
    dict(name='mer-comprobaciones',title='Modelo relacional 3 de 3',subtitle='Evidencia de CU-12. La autorización se vuelve a evaluar; no constituye un quinto estado.',width=1560,height=850,
    nodes=[
        node('v','VersionCotizacion · contexto',40,120,['PK id; FK pedido','decision']),
        node('c','ComprobacionSublimacion',600,120,['PK id','FK versionCotizacion','fecha; FK responsable','montoVerificado observado']),
        node('d','DisenoComprobado',1160,120,['PK/FK comprobacion','PK/FK versionDiseno']),
        node('u','Usuario · contexto',600,600,['PK id','rol Administrador']),
        node('vd','VersionDiseno · contexto',1160,600,['PK id; FK detalle','decision']),
    ],edges=[
        edge('v','c','1 → 0..*',[(360,165),(600,165)],(480,150)),
        edge('c','d','1 → 1..* diseños',[(920,165),(1160,165)],(1040,150)),
        edge('u','c','responsable: 1 → 0..*',[(760,600),(760,260)],(890,420)),
        edge('vd','d','versión: 1 → 0..*',[(1320,600),(1320,215)],(1410,420)),
    ]),
    dict(name='arquitectura',title='Arquitectura de Impacto Textil',subtitle='Implementados localmente: acceso, catálogo y administración de pedidos. CU-05 a CU-08 pendientes de integración.',width=1560,height=950,
    nodes=[
        node('b','Vista en el navegador',40,140,['HTML, CSS y JavaScript','Cliente o Administrador','Mismo origen; interfaz adaptable']),
        node('c','Controladores ASP.NET Core',600,140,['DTO de entrada y salida','Autenticación y autorización','Errores comprensibles']),
        node('s','Servicios del modelo',1160,140,['Reglas del SRS','Transacciones y concurrencia','Titularidad y versiones vigentes']),
        node('r','Repositorios y EF Core',1160,500,['Persistencia y consultas','DbContext compartido','Migraciones coordinadas']),
        node('db','SQL Server',600,500,['Relaciones y restricciones','Historial y pagos','Datos privados']),
        node('f','Servicio de archivos',1160,760,['Validar contenido y tamaño','Descarga autorizada']),
        node('a','Almacenamiento privado',600,760,['Fuera del contenido público','Originales y comprobantes']),
    ],edges=[
        edge('b','c','HTTPS · JSON / archivos',[(360,190),(600,190)],(480,175)),
        edge('c','s','Invoca el servicio',[(920,190),(1160,190)],(1040,175)),
        edge('s','r','Consulta / persiste',[(1320,260),(1320,500)],(1415,390)),
        edge('r','db','SQL',[(1160,555),(920,555)],(1040,540)),
        edge('s','f','Archivos autorizados',[(1480,230),(1520,230),(1520,800),(1480,800)],(1425,690)),
        edge('f','a','Referencia privada',[(1160,810),(920,810)],(1040,795)),
    ]),
    dict(name='despliegue',title='Despliegue previsto',subtitle='MonsterASP.NET Free y HTTPS público pendientes de prueba. No se afirma un sitio ya publicado.',width=1560,height=1000,
    nodes=[
        node('n','Navegador',40,150,['Escritorio y móvil','Usuario del sistema']),
        node('h','Subdominio y HTTPS',600,150,['Proveedor MonsterASP.NET','Certificado por verificar']),
        node('w','Aplicación publicada',1160,150,['ASP.NET Core y frontend','Runtime .NET 10 por comprobar']),
        node('sql','SQL Server del proveedor',1160,520,['Conexión configurada como secreto','Cuota y compatibilidad por probar']),
        node('f','Ruta privada del proveedor',600,520,['Persistencia por comprobar','Acceso desde la aplicación']),
        node('dev','Estación de desarrollo',40,780,['Git; rama Carlos_Borrell','SDK .NET 10; SQL Server local']),
    ],edges=[
        edge('n','h','HTTPS',[(360,195),(600,195)],(480,180)),
        edge('h','w','Mismo origen',[(920,195),(1160,195)],(1040,180)),
        edge('w','sql','Conexión SQL segura',[(1320,245),(1320,520)],(1420,385)),
        edge('w','f','Archivos privados',[(1160,230),(1040,230),(1040,565),(920,565)],(1040,400)),
        edge('dev','w','Paquete generado y configuración',[(360,825),(1515,825),(1515,200),(1480,200)],(880,850)),
    ]),
]

OUT.mkdir(parents=True, exist_ok=True)
mxfile = Element('mxfile', host='app.diagrams.net', type='device')
for page in pages:
    width, height = page['width'], page['height']
    svg = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">',
           '<defs><marker id="arrow" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M0 0 L10 5 L0 10 Z" fill="#41556b"/></marker></defs>',
           f'<rect width="{width}" height="{height}" fill="white"/>',
           f'<text x="40" y="40" font-family="Arial" font-size="27" font-weight="bold" fill="#172b40">{escape(page["title"])}</text>',
           f'<text x="40" y="70" font-family="Arial" font-size="16" fill="#425466">{escape(page["subtitle"])}</text>']
    diagram = SubElement(mxfile, 'diagram', id=page['name'], name=page['title'])
    model = SubElement(diagram,'mxGraphModel',page='1',pageWidth=str(width),pageHeight=str(height),grid='1',gridSize='10')
    root = SubElement(model,'root')
    SubElement(root,'mxCell',id='0')
    SubElement(root,'mxCell',id='1',parent='0')
    def text_cell(key, value, x, y, w, h, size=16, bold=False, centered=False):
        style = f'text;html=1;whiteSpace=wrap;align={"center" if centered else "left"};verticalAlign=middle;fontSize={size};fontColor=#172B40;fontStyle={1 if bold else 0};fillColor=#FFFFFF;strokeColor=none;'
        cell = SubElement(root, 'mxCell', id=key, value=escape(value), vertex='1', parent='1', style=style)
        SubElement(cell, 'mxGeometry', x=str(x), y=str(y), width=str(w), height=str(h), **{'as':'geometry'})
    text_cell('title', page['title'], 40, 12, width-80, 34, 27, True)
    text_cell('subtitle', page['subtitle'], 40, 48, width-80, 30)
    footer = 'Impacto Textil · Diseño contrastado con la rama Carlos_Borrell · 1 de noviembre de 2026'
    text_cell('footer', footer, 40, height-44, width-80, 28, 14)
    by_key = {n['key']: n for n in page['nodes']}
    for number, e in enumerate(page['edges']):
        pts = ' '.join(f'{x},{y}' for x,y in e['points'])
        svg.append(f'<polyline points="{pts}" fill="none" stroke="#41556b" stroke-width="2" marker-end="url(#arrow)"/>')
        x,y=e['label_at']; label=escape(e['label'])
        svg.append(f'<text x="{x}" y="{y}" text-anchor="middle" font-family="Arial" font-size="15" fill="#172b40" stroke="white" stroke-width="6" paint-order="stroke">{label}</text>')
        source, target = by_key[e['source']], by_key[e['target']]
        start, end = e['points'][0], e['points'][-1]
        ports = f'exitX={(start[0]-source["x"])/source["w"]};exitY={(start[1]-source["y"])/source["h"]};exitPerimeter=0;entryX={(end[0]-target["x"])/target["w"]};entryY={(end[1]-target["y"])/target["h"]};entryPerimeter=0;'
        cell=SubElement(root,'mxCell',id=f'e{number}',value='',edge='1',parent='1',source=e['source'],target=e['target'],style=ports+'edgeStyle=segmentEdgeStyle;endArrow=classic;html=1;rounded=0;strokeColor=#41556B;')
        geom=SubElement(cell,'mxGeometry',relative='1',**{'as':'geometry'})
        points=SubElement(geom,'Array',**{'as':'points'})
        for px,py in e['points'][1:-1]: SubElement(points,'mxPoint',x=str(px),y=str(py))
        label_width = max(100, len(e['label'])*8)
        text_cell(f'label{number}', e['label'], x-label_width/2, y-17, label_width, 23, 15, centered=True)
    for n in page['nodes']:
        x,y,w,h=n['x'],n['y'],n['w'],n['h']
        svg.extend([f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="5" fill="#f7f9fc" stroke="#70859b" stroke-width="1.5"/>',
                    f'<rect x="{x}" y="{y}" width="{w}" height="36" rx="5" fill="#193c58"/>',
                    f'<text x="{x+14}" y="{y+24}" font-family="Arial" font-size="18" font-weight="bold" fill="white">{escape(n["title"])}</text>'])
        for i,field in enumerate(n['fields']):
            svg.append(f'<text x="{x+14}" y="{y+57+i*23}" font-family="Arial" font-size="16" fill="#172b40">{escape(field)}</text>')
        value='<b>'+escape(n['title'])+'</b><hr>'+'<br>'.join(escape(f) for f in n['fields'])
        cell=SubElement(root,'mxCell',id=n['key'],value=value,vertex='1',parent='1',style='rounded=1;whiteSpace=wrap;html=1;align=left;verticalAlign=top;spacing=12;fontSize=16;fillColor=#F7F9FC;strokeColor=#70859B;')
        SubElement(cell,'mxGeometry',x=str(x),y=str(y),width=str(w),height=str(h),**{'as':'geometry'})
    svg.append(f'<text x="40" y="{height-24}" font-family="Arial" font-size="14" fill="#425466">{escape(footer)}</text></svg>')
    (OUT/(page['name']+'.svg')).write_text('\n'.join(svg),encoding='utf-8')
ElementTree(mxfile).write(OUT/'Impacto_Textil_Fase_3.drawio',encoding='utf-8',xml_declaration=True)
print('Generadas cinco figuras SVG y cinco páginas editables en draw.io.')
