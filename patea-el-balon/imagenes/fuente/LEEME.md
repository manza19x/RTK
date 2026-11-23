# Fuente de las imágenes

`art.html` dibuja el ícono y las miniaturas con SVG (usa `parts.js`).
Para volver a generarlas, abre `art.html` en Chrome y toma una captura de cada recuadro,
o usa Playwright:

```bash
node -e "const {chromium}=require('playwright');(async()=>{const b=await chromium.launch();const p=await b.newPage({viewport:{width:1920,height:2800}});await p.goto('file://'+process.cwd()+'/art.html');await p.evaluate(()=>document.fonts.ready);for(const id of ['icon','thumb1','thumb2'])await (await p.\$('#'+id)).screenshot({path:id+'.png'});await b.close()})()"
```
