const fs = require('fs');
const path = require('path');
const {chromium} = require('playwright');
(async () => {
  const base = path.resolve(__dirname,'..');
  const xml = fs.readFileSync(path.join(base,'docs','ERD_MeetingScheduler.drawio'),'utf8');
  const browser = await chromium.launch({channel:'msedge',headless:true});
  try {
    const page = await browser.newPage({viewport:{width:1650,height:1350},deviceScaleFactor:1.4});
    await page.setContent('<body style="margin:0;background:white"><div class="mxgraph" style="border:0"></div></body>');
    await page.evaluate(xml => {
      window.urlParams={};
      document.querySelector('.mxgraph').setAttribute('data-mxgraph',JSON.stringify({xml,nav:false,resize:true,toolbar:'',zoom:1,border:25}));
    },xml);
    await page.addScriptTag({path:path.join(process.env.TEMP,'fhou-viewer.min.js')});
    await page.evaluate(() => GraphViewer.processElements());
    await page.waitForFunction(() => document.querySelectorAll('svg g').length > 30);
    await page.locator('.mxgraph').screenshot({path:path.join(base,'docs','ERD_MeetingScheduler.png')});
    console.log(await page.locator('.mxgraph').boundingBox());
    console.log('Draw.io GraphViewer rendered native editable XML successfully.');
    const editor = await browser.newPage({viewport:{width:1400,height:1100},ignoreHTTPSErrors:true});
    await editor.setContent('<body style="margin:0"><iframe id="editor" style="width:100vw;height:100vh;border:0"></iframe></body>');
    await editor.evaluate(xml => {
      window.editorEvents=[];
      const frame=document.querySelector('#editor');
      window.addEventListener('message', e => {
        if(e.source !== frame.contentWindow || !['https://embed.diagrams.net','https://app.diagrams.net'].includes(e.origin)) return;
        let msg; try {msg=JSON.parse(e.data);} catch {return;}
        window.editorEvents.push(msg);
        if(msg.event==='init') frame.contentWindow.postMessage(JSON.stringify({action:'load',xml,title:'ERD_MeetingScheduler.drawio',fit:1}),e.origin);
      });
      frame.src='https://embed.diagrams.net/?embed=1&proto=json&spin=1&ui=min&lang=vi';
    },xml);
    const editorLoaded = await editor.waitForFunction(() => window.editorEvents.some(e=>e.event==='load'),{},{timeout:30000}).then(()=>true).catch(async () => {
      console.log('Editor diagnostics:',await editor.evaluate(()=>window.editorEvents));
      console.log('Frame URLs:',editor.frames().map(f=>f.url()));
      console.log('Online editor validation unavailable; local XML rendering succeeded.');
      return false;
    });
    if (editorLoaded) {
      console.log('diagrams.net editor opened XML:', await editor.evaluate(() => window.editorEvents.find(e=>e.event==='load')));
      await editor.screenshot({path:path.join(base,'docs','qa','erd-editor-check.png')});
    }
  } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
