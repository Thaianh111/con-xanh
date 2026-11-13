import { NodeIO, Document } from '@gltf-transform/core';
import { mergeDocuments, prune, dedup, unpartition, getBounds } from '@gltf-transform/functions';
import fs from 'fs';
const io = new NodeIO();
const NAT = '/home/claude/demo/assets/nature/', TEX = '/home/claude/demo/assets/tex/';
const KK = '/home/claude/assets_src/kk/addons/kaykit_medieval_hexagon_pack/Assets/gltf/';
const pack = new Document(); pack.createBuffer();
const outScene = pack.createScene('all');
const nature = ['NormalTree_3','PalmTree_1','PalmTree_2','PalmTree_3','PineTree_1','PineTree_3','MapleTree_1','Bush','Bush_Flowers','Bush_Large_Flowers','Flower_3_Clump','Flower_4_Clump','Grass_Small','Plant_1','Plant_Flowers'];
const kk = {home_A:'buildings/blue/building_home_A_blue',home_B:'buildings/blue/building_home_B_blue',well:'buildings/blue/building_well_blue',windmill:'buildings/blue/building_windmill_blue',watermill:'buildings/blue/building_watermill_blue',market:'buildings/blue/building_market_blue',fence:'buildings/neutral/fence_wood_straight',bridge:'buildings/neutral/building_bridge_A',barrel:'decoration/props/barrel',crate:'decoration/props/crate_A_small',sack:'decoration/props/sack',bucket:'decoration/props/bucket_water',wheelbarrow:'decoration/props/wheelbarrow',lumber:'decoration/props/resource_lumber',rockA:'decoration/nature/rock_single_A',rockC:'decoration/nature/rock_single_C',lily:'decoration/nature/waterlily_A',reed:'decoration/nature/waterplant_A'};
async function add(name, path, zup) {
  const d = await io.read(path);
  const sc = d.getRoot().listScenes()[0];
  const wrap = d.createNode(name);
  if (zup) wrap.setRotation([-Math.SQRT1_2, 0, 0, Math.SQRT1_2]);
  for (const c of sc.listChildren()) { sc.removeChild(c); wrap.addChild(c); }
  sc.addChild(wrap);
  const map = mergeDocuments(pack, d);
  const newScene = map.get(sc);
  for (const c of newScene.listChildren()) { newScene.removeChild(c); outScene.addChild(c); }
  newScene.dispose();
}
for (const n of nature) await add(n, NAT + n + '.glb', n !== 'MapleTree_1');
for (const [k, p] of Object.entries(kk)) await add(k, KK + p + '.gltf', false);
// textures for nature materials
const texCache = {};
const tex = f => texCache[f] ??= pack.createTexture(f).setImage(fs.readFileSync(TEX + f)).setMimeType('image/png');
for (const m of pack.getRoot().listMaterials()) {
  const n = m.getName();
  const file = { NormalTree_Bark:'NormalTree_Bark.png', NormalTree_Leaves:'NormalTree_Leaves.png', PalmTree_Trunk:'PalmTree_Trunk.png', PalmTree_Leaves:'PalmTree_Leaves.png', PineTree_Bark:'PineTree_Bark.png', PineTree_Leaves:'PineTree_Leaves.png', BirchTree_Bark:'BirchTree_Bark.png', BirchTree_Leaves:'BirchTree_Leaves.png', MapleTree_Bark:'MapleTree_Bark.png', MapleTree_Leaves:'MapleTree_Leaves.png', Bush_Leaves:'Bush_Leaves.png', Flowers:'Flowers.png', Grass:'Grass.png' }[n];
  if (file) {
    m.setBaseColorTexture(tex(file)).setBaseColorFactor([1,1,1,1]).setMetallicFactor(0).setRoughnessFactor(0.9);
    if (/Leaves|Flowers|Grass/.test(n)) m.setAlphaMode('MASK').setAlphaCutoff(0.5).setDoubleSided(true);
  }
}
for (const s of pack.getRoot().listScenes()) if (s !== outScene) s.dispose();
pack.getRoot().setDefaultScene(outScene);
await pack.transform(dedup(), prune(), unpartition());
// report bounds
for (const n of outScene.listChildren()) { const b = getBounds(n); console.log(n.getName(), b.min.map(v=>v.toFixed(2)).join(','), '|', b.max.map(v=>v.toFixed(2)).join(',')); }
await io.write('/home/claude/demo/assets/pack.glb', pack);
console.log(fs.statSync('/home/claude/demo/assets/pack.glb').size);
