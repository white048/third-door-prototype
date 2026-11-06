import http from 'node:http';
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../Documentation/ArtDirection');
const mime={'.html':'text/html; charset=utf-8','.json':'application/json; charset=utf-8','.txt':'text/plain; charset=utf-8','.svg':'image/svg+xml','.png':'image/png','.ttf':'font/ttf'};
http.createServer(async(req,res)=>{
  try{
    let name=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname);
    if(name==='/')name='/index.html';
    const file=path.resolve(root,'.'+name);
    if(!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
    if(!(await fs.stat(file)).isFile())throw new Error('not a file');
    const body=await fs.readFile(file);res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});res.end(body);
  }catch{res.writeHead(404);res.end('Not found');}
}).listen(4785,'127.0.0.1',()=>console.log('ART BRIEF / http://127.0.0.1:4785'));
