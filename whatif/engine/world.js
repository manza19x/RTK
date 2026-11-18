import * as THREE from 'three';

export const W = 1080, H = 1920;

// ---------- utils ----------
export function rng(seed) {
  let a = seed >>> 0;
  return () => {
    a |= 0; a = (a + 0x6D2B79F5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
export const clamp = (x, a = 0, b = 1) => Math.max(a, Math.min(b, x));
export const lerp = (a, b, t) => a + (b - a) * t;
export const smooth = (t) => { t = clamp(t); return t * t * (3 - 2 * t); };
export const ease = (t) => { t = clamp(t); return t < .5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2; };
export const easeOut = (t) => 1 - Math.pow(1 - clamp(t), 3);
export const easeIn = (t) => Math.pow(clamp(t), 3);
// progress of t inside [a,b]
export const prog = (t, a, b) => clamp((t - a) / (b - a));
export const col = (c) => new THREE.Color(c);
export function mixColor(a, b, t) { return new THREE.Color(a).lerp(new THREE.Color(b), clamp(t)); }

// keyframe interpolation of plain objects / numbers / arrays
export function keys(t, ks, fn = ease) {
  if (t <= ks[0][0]) return ks[0][1];
  for (let i = 0; i < ks.length - 1; i++) {
    const [t0, v0] = ks[i], [t1, v1] = ks[i + 1];
    if (t <= t1) {
      const k = fn((t - t0) / (t1 - t0));
      if (Array.isArray(v0)) return v0.map((x, j) => lerp(x, v1[j], k));
      if (typeof v0 === 'number') return lerp(v0, v1, k);
      return k < .5 ? v0 : v1;
    }
  }
  return ks[ks.length - 1][1];
}

// ---------- shaders ----------
const skyVert = `varying vec3 vDir; void main(){ vDir = position; gl_Position = projectionMatrix*modelViewMatrix*vec4(position,1.); }`;
const skyFrag = `
uniform vec3 top; uniform vec3 hor; uniform vec3 bot; uniform vec3 sunDir; uniform vec3 sunCol; uniform float sunGlow;
varying vec3 vDir;
void main(){
  vec3 d = normalize(vDir); float h = d.y;
  vec3 c = h > 0. ? mix(hor, top, pow(clamp(h*1.6,0.,1.), 0.7)) : mix(hor, bot, clamp(-h*6.,0.,1.));
  float s = max(dot(d, normalize(sunDir)), 0.);
  c += sunCol * (pow(s, 600.)*2.5 + pow(s, 40.)*0.35 + pow(s, 6.)*0.12) * sunGlow;
  gl_FragColor = vec4(c, 1.);
}`;

const seaVert = `
uniform float time, level, waveAmp, tsuZ, tsuH, tsuW, tsuX;
varying vec3 vW; varying float vTsu;
void main(){
  vec4 wp = modelMatrix * vec4(position, 1.);
  float y = level;
  y += waveAmp*(sin(wp.x*0.35 + time*1.3)*0.35 + sin(wp.z*0.45 - time*1.7)*0.5 + sin((wp.x + wp.z)*0.9 + time*2.3)*0.18);
  float dz = (tsuX > 0.5 ? wp.x : wp.z) - tsuZ;
  float bump = dz > 0. ? exp(-dz*dz/(tsuW*tsuW*0.08)) : exp(-dz*dz/(tsuW*tsuW));
  y += tsuH * bump; vTsu = bump * step(0.01, tsuH);
  wp.y = y; vW = wp.xyz;
  gl_Position = projectionMatrix * viewMatrix * wp;
}`;
const seaFrag = `
uniform vec3 shallow, deep, foamCol, fogCol, sunDir, ice;
uniform float fogNear, fogFar, time, slopeSea, slopeLand, iceAmt, light, foamAmt, dim;
varying vec3 vW; varying float vTsu;
float groundH(float z){ return z < 0. ? z*slopeSea : z*slopeLand; }
void main(){
  float depth = vW.y - groundH(vW.z);
  vec3 c = mix(shallow, deep, smoothstep(0.3, 3.5, depth));
  vec3 n = normalize(cross(dFdx(vW), dFdy(vW))); if (n.y < 0.) n = -n;
  vec3 L = normalize(sunDir);
  c *= 0.7 + 0.45*max(dot(n, L), 0.);
  vec3 v = normalize(cameraPosition - vW);
  c += pow(max(dot(n, normalize(v + L)), 0.), 90.) * 0.5 * light;
  float f = smoothstep(0.28, 0.0, depth);
  float st = smoothstep(0.62, 0.95, sin(vW.z*1.4 - time*1.8 + sin(vW.x*0.25)*1.6)*0.5 + 0.5) * smoothstep(1.4, 0.15, depth);
  float st2 = smoothstep(0.75, 1.0, sin(vW.x*0.7 + vW.z*0.3 + time*0.9 + sin(vW.z*0.2+time*0.3)*3.)*0.5+0.5) * 0.25;
  float fm = clamp(f + st*0.85 + st2, 0., 1.) * foamAmt;
  fm = max(fm, vTsu * smoothstep(0.3, 0.9, sin(vW.x*1.3 + vW.y*3. + time*4.)*0.5+0.5) * 0.8);
  c = mix(c, foamCol*light, fm);
  c = mix(c, ice*(0.75+0.25*max(dot(n,L),0.)), iceAmt);
  c *= dim;
  float dist = length(vW - cameraPosition);
  c = mix(c, fogCol, smoothstep(fogNear, fogFar, dist));
  gl_FragColor = vec4(c, 1.);
}`;

// ---------- world ----------
export class World {
  constructor(canvas) {
    this.renderer = new THREE.WebGLRenderer({ canvas, antialias: true, preserveDrawingBuffer: true });
    const qp = new URLSearchParams(location.search);
    const gs = +(qp.get('gl') || 1);
    this.renderer.setPixelRatio(1);
    this.renderer.setSize(Math.round(W * gs), Math.round(H * gs), false);
    this.renderer.shadowMap.enabled = qp.get('sh') !== '0';
    this.renderer.shadowMap.type = qp.get('soft') === '0' ? THREE.PCFShadowMap : THREE.PCFSoftShadowMap;
    this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
    this.renderer.toneMappingExposure = 1.0;
    this.renderer.outputColorSpace = THREE.SRGBColorSpace;

    const s = this.scene = new THREE.Scene();
    this.camera = new THREE.PerspectiveCamera(55, W / H, 0.1, 3000);
    this.fog = new THREE.Fog(0xcfe3ee, 60, 420);
    s.fog = this.fog;

    this.hemi = new THREE.HemisphereLight(0xdfefff, 0xc9b48f, 1.3);
    s.add(this.hemi);
    this.sun = new THREE.DirectionalLight(0xfff3e0, 2.4);
    this.sun.castShadow = true;
    this.sun.shadow.mapSize.set(+(qp.get('sm') || 2048), +(qp.get('sm') || 2048));
    const sc = this.sun.shadow.camera; sc.left = -45; sc.right = 45; sc.top = 45; sc.bottom = -45; sc.near = 1; sc.far = 300;
    this.sun.shadow.bias = -0.0004; this.sun.shadow.normalBias = 0.02;
    s.add(this.sun); s.add(this.sun.target);
    this.sunDir = new THREE.Vector3(0.4, 0.6, -0.7).normalize();

    // sky
    this.skyU = {
      top: { value: col(0x3d7fc4) }, hor: { value: col(0xd9ecf5) }, bot: { value: col(0xcfe3ee) },
      sunDir: { value: this.sunDir.clone() }, sunCol: { value: col(0xfff2d8) }, sunGlow: { value: 1 },
    };
    this.sky = new THREE.Mesh(new THREE.SphereGeometry(1500, 48, 24),
      new THREE.ShaderMaterial({ uniforms: this.skyU, vertexShader: skyVert, fragmentShader: skyFrag, side: THREE.BackSide, depthWrite: false, fog: false }));
    this.sky.renderOrder = -10;
    s.add(this.sky);

    this.slopeSea = 0.03; this.slopeLand = 0.008;
    // ground
    const gg = new THREE.PlaneGeometry(1600, 1600, 160, 160); gg.rotateX(-Math.PI / 2);
    const pos = gg.attributes.position;
    for (let i = 0; i < pos.count; i++) pos.setY(i, this.groundH(pos.getZ(i)));
    gg.computeVertexNormals();
    const cols = new Float32Array(pos.count * 3);
    const r = rng(7);
    for (let i = 0; i < pos.count; i++) { const v = 0.94 + r() * 0.06; cols[i * 3] = v; cols[i * 3 + 1] = v; cols[i * 3 + 2] = v; }
    gg.setAttribute('color', new THREE.BufferAttribute(cols, 3));
    this.groundMat = new THREE.MeshStandardMaterial({ color: 0xcdbb9c, roughness: 1, vertexColors: true });
    this.ground = new THREE.Mesh(gg, this.groundMat);
    this.ground.receiveShadow = true;
    s.add(this.ground);

    // sea
    this.seaU = {
      time: { value: 0 }, level: { value: 0 }, waveAmp: { value: 0.12 },
      tsuZ: { value: -9999 }, tsuH: { value: 0 }, tsuW: { value: 30 }, tsuX: { value: 0 },
      shallow: { value: col(0x3fd0c4) }, deep: { value: col(0x1b6f8f) }, foamCol: { value: col(0xf4fbfb) },
      fogCol: { value: this.fog.color }, fogNear: { value: 60 }, fogFar: { value: 420 },
      sunDir: { value: this.sunDir }, slopeSea: { value: this.slopeSea }, slopeLand: { value: this.slopeLand },
      ice: { value: col(0xdfeef5) }, iceAmt: { value: 0 }, light: { value: 1 }, foamAmt: { value: 1 }, dim: { value: 1 },
    };
    const sg = new THREE.PlaneGeometry(1600, 1600, +(qp.get("seg") || 400), +(qp.get("seg") || 400)); sg.rotateX(-Math.PI / 2);
    this.sea = new THREE.Mesh(sg, new THREE.ShaderMaterial({ uniforms: this.seaU, vertexShader: seaVert, fragmentShader: seaFrag }));
    this.sea.position.z = -500; // most of it out at sea
    s.add(this.sea);

    // stars
    const sr = rng(99), sp = [];
    for (let i = 0; i < 2500; i++) {
      const u = sr() * 2 - 1, th = sr() * Math.PI * 2, rr = Math.sqrt(1 - u * u);
      const y = Math.abs(u) * 0.95 + 0.02;
      sp.push(rr * Math.cos(th) * 1200, y * 1200, rr * Math.sin(th) * 1200);
    }
    const sgeo = new THREE.BufferGeometry(); sgeo.setAttribute('position', new THREE.Float32BufferAttribute(sp, 3));
    this.starMat = new THREE.PointsMaterial({ color: 0xffffff, size: 2.4, sizeAttenuation: false, transparent: true, opacity: 0, fog: false, depthWrite: false });
    this.stars = new THREE.Points(sgeo, this.starMat); this.stars.renderOrder = -9;
    s.add(this.stars);

    // celestial bodies (billboard-ish spheres far away)
    this.moon = new THREE.Mesh(new THREE.SphereGeometry(24, 32, 16), new THREE.MeshBasicMaterial({ map: moonTexture(), fog: false, transparent: true }));
    this.moon.visible = false; s.add(this.moon);
    this.sunDisc = new THREE.Mesh(new THREE.SphereGeometry(18, 24, 12), new THREE.MeshBasicMaterial({ color: 0xfff6dd, fog: false, transparent: true }));
    this.sunDisc.visible = false; s.add(this.sunDisc);

    // clouds
    this.clouds = new THREE.Group();
    const cr = rng(31), cm = new THREE.MeshLambertMaterial({ color: 0xffffff, flatShading: true, fog: false, transparent: true, opacity: 0.95, emissive: 0x8899aa, emissiveIntensity: 0.35 });
    this.cloudMat = cm;
    for (let i = 0; i < 26; i++) {
      const g = new THREE.Group();
      const n = 3 + Math.floor(cr() * 4);
      for (let j = 0; j < n; j++) {
        const m = new THREE.Mesh(new THREE.IcosahedronGeometry(1, 0), cm);
        m.position.set((j - n / 2) * 1.3 + cr(), cr() * 0.6, cr() * 1.2);
        const sz = 1 + cr() * 1.1; m.scale.set(sz * 1.3, sz * 0.7, sz);
        g.add(m);
      }
      const a = cr() * Math.PI * 2, d = 500 + cr() * 500;
      g.position.set(Math.cos(a) * d, 90 + cr() * 140, Math.sin(a) * d);
      g.scale.setScalar(16 + cr() * 18);
      g.lookAt(0, g.position.y, 0);
      this.clouds.add(g);
    }
    s.add(this.clouds);

    this.extra = new THREE.Group(); s.add(this.extra);
    this.time = 0;
    this.shake = 0; this.roll = 0;
  }

  groundH(z) { return z < 0 ? z * this.slopeSea : z * this.slopeLand; }
  reshapeGround(slopeSea, slopeLand) {
    this.slopeSea = slopeSea; this.slopeLand = slopeLand;
    this.seaU.slopeSea.value = slopeSea; this.seaU.slopeLand.value = slopeLand;
    const pos = this.ground.geometry.attributes.position;
    for (let i = 0; i < pos.count; i++) pos.setY(i, this.groundH(pos.getZ(i)));
    pos.needsUpdate = true; this.ground.geometry.computeVertexNormals();
  }
  // far "carpet" of people seen from above, outside rect [x0,x1]x[z0,z1]
  // far "carpet" of people (cheap texture) outside rect [x0,x1]x[z0,z1], plus floor under the real crowd
  crowdCarpet(x0, x1, z0, z1, ext = 700) {
    const N = 1024, TILE = 18; // metres per tile
    const cv = document.createElement('canvas'); cv.width = cv.height = N;
    const g = cv.getContext('2d'); g.fillStyle = '#5d5247'; g.fillRect(0, 0, N, N);
    const r = rng(77), C = ['#e63b3b', '#f2d03b', '#3b8fe6', '#3fc46a', '#8e44c9', '#f08a2b', '#f27ab0', '#2ec4c4', '#f5f5f0', '#2f5fd0', '#95d13a', '#d94f7a'];
    const ppm = N / TILE, cnt = Math.round(TILE * TILE * 1.6);
    for (let i = 0; i < cnt; i++) {
      const x = r() * N, y = r() * N, w = 0.46 * ppm, h = 0.3 * ppm;
      for (const dx of [0, -N, N]) for (const dy of [0, -N, N]) {
        g.fillStyle = 'rgba(30,22,18,.45)'; g.fillRect(x + dx + 4, y + dy + 6, w, h);
        g.fillStyle = C[Math.floor(r() * C.length)]; g.fillRect(x + dx, y + dy, w, h);
        g.fillStyle = ['#f1c9a5', '#c68a5f', '#6e4a2f', '#2a1d14', '#2a1d14'][Math.floor(r() * 5)];
        g.beginPath(); g.arc(x + dx + w / 2, y + dy + h / 2, 0.17 * ppm, 0, 7); g.fill();
      }
    }
    const tex = new THREE.CanvasTexture(cv); tex.wrapS = tex.wrapT = THREE.RepeatWrapping; tex.colorSpace = THREE.SRGBColorSpace;
    tex.anisotropy = 8;
    const grp = new THREE.Group();
    const add = (ax, bx, az, bz, y) => {
      const w = bx - ax, d = bz - az; if (w <= 0 || d <= 0) return;
      const t = tex.clone(); t.needsUpdate = true; t.repeat.set(w / TILE, d / TILE);
      const m = new THREE.Mesh(new THREE.PlaneGeometry(w, d).rotateX(-Math.PI / 2), new THREE.MeshStandardMaterial({ map: t, roughness: 1 }));
      m.position.set((ax + bx) / 2, y, (az + bz) / 2); m.receiveShadow = true; grp.add(m);
    };
    add(-ext, ext, -ext, z0, 1.0); add(-ext, ext, z1, ext, 1.0); add(-ext, x0, z0, z1, 1.0); add(x1, ext, z0, z1, 1.0);
    const fl = new THREE.Mesh(new THREE.PlaneGeometry(x1 - x0, z1 - z0).rotateX(-Math.PI / 2), new THREE.MeshStandardMaterial({ color: 0x8c7b63, roughness: 1 }));
    fl.position.set((x0 + x1) / 2, 0.02, (z0 + z1) / 2); fl.receiveShadow = true; grp.add(fl);
    this.scene.add(grp); return grp;
  }

  // presets
  setDay() {
    this.setSky(0x3d7fc4, 0xd9ecf5, 0xcfe3ee); this.light(1);
  }
  setSky(top, hor, fog) {
    this.skyU.top.value.set(top); this.skyU.hor.value.set(hor);
    this.fog.color.set(fog); this.skyU.bot.value.set(fog);
  }
  light(k, hemiK = k) {
    this.sun.intensity = 2.4 * k; this.hemi.intensity = 1.3 * hemiK;
    this.seaU.light.value = Math.max(0.15, k);
    this.seaU.dim.value = Math.min(1, Math.max(0.1, (k * 0.7 + hemiK * 0.3)));
  }
  setFog(near, far) { this.fog.near = near; this.fog.far = far; this.seaU.fogNear.value = near; this.seaU.fogFar.value = far; }
  setSunDir(x, y, z) { this.sunDir.set(x, y, z).normalize(); this.skyU.sunDir.value.copy(this.sunDir); }

  cam(pos, look, fov = 55) {
    const c = this.camera;
    c.fov = fov; c.updateProjectionMatrix();
    let [x, y, z] = pos;
    if (this.shake > 0) {
      const t = this.time;
      x += Math.sin(t * 71.3) * this.shake * 0.6 + Math.sin(t * 33.1) * this.shake * 0.4;
      y += Math.sin(t * 57.7) * this.shake * 0.5;
      z += Math.sin(t * 43.9) * this.shake * 0.3;
    }
    // subtle handheld drift
    x += Math.sin(this.time * 0.7) * 0.05; y += Math.sin(this.time * 0.9 + 1) * 0.04;
    c.position.set(x, y, z);
    c.up.set(Math.sin(this.roll), Math.cos(this.roll), 0);
    c.lookAt(look[0], look[1], look[2]);
  }

  render(t) {
    this.time = t;
    this.seaU.time.value = t;
    const c = this.camera;
    this.sky.position.copy(c.position);
    this.stars.position.copy(c.position);
    // shadow camera follows look target region
    const tgt = new THREE.Vector3(); c.getWorldDirection(tgt);
    const focus = c.position.clone().add(tgt.multiplyScalar(25)); focus.y = 0;
    this.sun.target.position.copy(focus);
    this.sun.position.copy(focus).add(this.sunDir.clone().multiplyScalar(120));
    this.clouds.rotation.y = t * 0.004;
    this.renderer.render(this.scene, c);
  }
}

function moonTexture() {
  const cv = document.createElement('canvas'); cv.width = 256; cv.height = 128;
  const g = cv.getContext('2d');
  g.fillStyle = '#e9e7df'; g.fillRect(0, 0, 256, 128);
  const r = rng(5);
  for (let i = 0; i < 60; i++) {
    const x = r() * 256, y = 10 + r() * 108, rad = 2 + r() * 14;
    g.fillStyle = `rgba(150,148,140,${0.25 + r() * 0.35})`;
    g.beginPath(); g.ellipse(x, y, rad, rad * 0.8, 0, 0, Math.PI * 2); g.fill();
  }
  const t = new THREE.CanvasTexture(cv); t.colorSpace = THREE.SRGBColorSpace; return t;
}

// ---------- crowd of blocky people ----------
const SHIRTS = [0xe63b3b, 0xf2d03b, 0x3b8fe6, 0x3fc46a, 0x8e44c9, 0xf08a2b, 0xf27ab0, 0x2ec4c4, 0xf5f5f0, 0x2f5fd0, 0xd94f7a, 0x95d13a, 0xffa75c, 0x5ad1e8];
const PANTS = [0x2c3e66, 0x3a3a44, 0xb59a6a, 0x5a6b7d, 0x1f2a44, 0x7d4d32, 0x4f6a3a, 0xd9d4c4];
const SKIN = [0xf1c9a5, 0xe0ac82, 0xc68a5f, 0x9c6a43, 0x6e4a2f, 0xf6d7bd, 0xd29b6c];
const HAIR = [0x2a1d14, 0x4a3020, 0x111111, 0x8a6a3a, 0xd8c08a, 0x6b3a1e, 0xb8b8b8];

export class Crowd {
  constructor(world, n, seed = 1) {
    this.world = world; this.n = n;
    const r = this.r = rng(seed);
    const mat = new THREE.MeshStandardMaterial({ roughness: 0.85, metalness: 0 });
    const mk = (geo, count) => {
      const m = new THREE.InstancedMesh(geo, mat, count);
      m.castShadow = true; m.receiveShadow = true;
      m.instanceMatrix.setUsage(THREE.DynamicDrawUsage);
      m.frustumCulled = false;
      world.scene.add(m); return m;
    };
    this.legs = mk(new THREE.BoxGeometry(0.34, 0.82, 0.22).translate(0, 0.41, 0), n);
    this.torso = mk(new THREE.BoxGeometry(0.48, 0.62, 0.28).translate(0, 1.13, 0), n);
    this.head = mk(new THREE.SphereGeometry(0.17, 10, 8).scale(1, 1.08, 1).translate(0, 1.62, 0), n);
    this.hair = mk(new THREE.SphereGeometry(0.178, 10, 6, 0, Math.PI * 2, 0, Math.PI * 0.5).translate(0, 1.645, -0.01), n);
    this.arms = mk(new THREE.BoxGeometry(0.13, 0.6, 0.13).translate(0, -0.27, 0), n * 2);
    this.p = [];
    const c = new THREE.Color();
    for (let i = 0; i < n; i++) {
      const shirt = SHIRTS[Math.floor(r() * SHIRTS.length)], pants = PANTS[Math.floor(r() * PANTS.length)], skin = SKIN[Math.floor(r() * SKIN.length)];
      const hair = r() < 0.15 ? skin : HAIR[Math.floor(r() * HAIR.length)];
      this.legs.setColorAt(i, c.set(pants));
      this.torso.setColorAt(i, c.set(shirt));
      this.head.setColorAt(i, c.set(skin));
      this.hair.setColorAt(i, c.set(hair));
      this.arms.setColorAt(i * 2, c.set(r() < 0.6 ? shirt : skin));
      this.arms.setColorAt(i * 2 + 1, c.set(this.arms.instanceColor ? shirt : skin));
      this.p.push({ x: 0, z: 0, y: 0, ry: 0, rx: 0, rz: 0, lean: 0, armL: 0, armR: 0, s: 0.92 + r() * 0.16, vis: 1, rnd: r(), rnd2: r(), rnd3: r(), free: false });
    }
    // arms same colour as torso mostly
    for (let i = 0; i < n; i++) {
      const sh = new THREE.Color(); this.torso.getColorAt(i, sh);
      const sk = new THREE.Color(); this.head.getColorAt(i, sk);
      const short = this.p[i].rnd3 < 0.45;
      this.arms.setColorAt(i * 2, short ? sk : sh); this.arms.setColorAt(i * 2 + 1, short ? sk : sh);
    }
    this.m = new THREE.Matrix4(); this.m2 = new THREE.Matrix4(); this.q = new THREE.Quaternion(); this.e = new THREE.Euler();
    this.v = new THREE.Vector3(); this.sv = new THREE.Vector3();
  }
  // place on grid with jitter inside rect
  scatter(x0, x1, z0, z1, faceY = Math.PI) {
    const r = this.r;
    for (const p of this.p) {
      p.x = lerp(x0, x1, r()); p.z = lerp(z0, z1, r());
      p.ry = faceY + (r() - 0.5) * 0.9;
      p.bx = p.x; p.bz = p.z; p.bry = p.ry;
    }
    // simple relaxation so they don't overlap
    for (let it = 0; it < 3; it++) {
      const cell = new Map(), key = (x, z) => `${Math.floor(x)},${Math.floor(z)}`;
      this.p.forEach((p, i) => { const k = key(p.x, p.z); if (!cell.has(k)) cell.set(k, []); cell.get(k).push(i); });
      for (const p of this.p) {
        const cx = Math.floor(p.x), cz = Math.floor(p.z);
        for (let dx = -1; dx <= 1; dx++) for (let dz = -1; dz <= 1; dz++) {
          const l = cell.get(`${cx + dx},${cz + dz}`); if (!l) continue;
          for (const j of l) {
            const q = this.p[j]; if (q === p) continue;
            const ddx = p.x - q.x, ddz = p.z - q.z, d = Math.hypot(ddx, ddz);
            if (d < 0.7 && d > 1e-4) { const k = (0.7 - d) * 0.5; p.x += ddx / d * k; p.z += ddz / d * k; }
          }
        }
      }
    }
    for (const p of this.p) { p.bx = p.x; p.bz = p.z; }
    return this;
  }
  update() {
    const { m, m2, q, e, v, sv } = this;
    const zero = new THREE.Matrix4().makeScale(0, 0, 0);
    for (let i = 0; i < this.n; i++) {
      const p = this.p[i];
      if (!p.vis) {
        for (const mesh of [this.legs, this.torso, this.head, this.hair]) mesh.setMatrixAt(i, zero);
        this.arms.setMatrixAt(i * 2, zero); this.arms.setMatrixAt(i * 2 + 1, zero);
        continue;
      }
      const gy = p.free ? 0 : this.world.groundH(p.z);
      e.set(p.lean + p.rx, p.ry, p.rz, 'YXZ');
      q.setFromEuler(e);
      v.set(p.x, gy + p.y, p.z); sv.setScalar(p.s * (p.vis < 1 ? p.vis : 1));
      m.compose(v, q, sv);
      this.legs.setMatrixAt(i, m); this.torso.setMatrixAt(i, m); this.head.setMatrixAt(i, m); this.hair.setMatrixAt(i, m);
      for (let s = 0; s < 2; s++) {
        const side = s ? 1 : -1, a = s ? p.armR : p.armL;
        // a: 0 = down, 1 = straight up
        m2.makeRotationZ(side * (0.12 + a * 2.85));
        m2.setPosition(side * 0.3, 1.4, 0);
        this.arms.setMatrixAt(i * 2 + s, m.clone().multiply(m2));
      }
    }
    for (const mesh of [this.legs, this.torso, this.head, this.hair, this.arms]) mesh.instanceMatrix.needsUpdate = true;
  }
}

// generic instanced helper for props (buildings, rocks, trees, boats)
export function instanced(world, geo, matOpts, count) {
  const mat = new THREE.MeshStandardMaterial({ roughness: 0.9, ...matOpts });
  const m = new THREE.InstancedMesh(geo, mat, count);
  m.castShadow = true; m.receiveShadow = true; m.frustumCulled = false;
  world.scene.add(m); return m;
}
export { THREE };
