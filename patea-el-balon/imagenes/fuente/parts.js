// Piezas SVG reutilizables
function ball(cx, cy, r) {
  const pent = (x, y, s, rot) => {
    let pts = [];
    for (let i = 0; i < 5; i++) {
      const a = (rot + i * 72 - 90) * Math.PI / 180;
      pts.push((x + Math.cos(a) * s).toFixed(1) + ',' + (y + Math.sin(a) * s).toFixed(1));
    }
    return `<polygon points="${pts.join(' ')}" fill="#1c1c24"/>`;
  };
  const id = 'b' + Math.random().toString(36).slice(2, 7);
  let outer = '';
  for (let i = 0; i < 5; i++) {
    const a = (i * 72 - 90) * Math.PI / 180;
    outer += pent(cx + Math.cos(a) * r * 0.95, cy + Math.sin(a) * r * 0.95, r * 0.34, i * 72 + 36);
  }
  return `<defs><clipPath id="${id}"><circle cx="${cx}" cy="${cy}" r="${r}"/></clipPath>
  <radialGradient id="${id}g" cx="35%" cy="30%" r="75%"><stop offset="0" stop-color="#fff"/><stop offset="0.7" stop-color="#f0f0f4"/><stop offset="1" stop-color="#b9bcc8"/></radialGradient></defs>
  <circle cx="${cx}" cy="${cy}" r="${r}" fill="url(#${id}g)"/>
  <g clip-path="url(#${id})">${pent(cx, cy, r * 0.36, 0)}${outer}</g>
  <circle cx="${cx}" cy="${cy}" r="${r}" fill="none" stroke="#14121a" stroke-width="${r * 0.09}"/>
  <ellipse cx="${cx - r * 0.35}" cy="${cy - r * 0.45}" rx="${r * 0.28}" ry="${r * 0.14}" fill="#fff" opacity="0.7" transform="rotate(-30 ${cx - r * 0.35} ${cy - r * 0.45})"/>`;
}

// Estela de fuego detrás del balón (hacia la izquierda-abajo)
function trail(x1, y1, x2, y2, w, colors) {
  const id = 't' + Math.random().toString(36).slice(2, 7);
  const ang = Math.atan2(y2 - y1, x2 - x1);
  const nx = -Math.sin(ang) * w / 2, ny = Math.cos(ang) * w / 2;
  return `<defs><linearGradient id="${id}" gradientUnits="userSpaceOnUse" x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}">
    <stop offset="0" stop-color="${colors[0]}" stop-opacity="0"/><stop offset="0.6" stop-color="${colors[1]}" stop-opacity="0.85"/><stop offset="1" stop-color="${colors[2]}" stop-opacity="1"/></linearGradient></defs>
    <path d="M${x1},${y1} L${x2 + nx},${y2 + ny} Q${x2 + Math.cos(ang) * w * 0.7},${y2 + Math.sin(ang) * w * 0.7} ${x2 - nx},${y2 - ny} Z" fill="url(#${id})"/>`;
}

// Jugador de bloques pateando (mirando a la derecha). (x,y) = cadera, s = escala
function kicker(x, y, s, opt = {}) {
  const shirt = opt.shirt || '#e8343c', shorts = opt.shorts || '#ffffff', skin = '#f5cd30', boot = opt.boot || '#1b1b22', hair = opt.hair || '#3a2618';
  const num = opt.number || '10';
  const ol = `stroke="#14121a" stroke-width="${5 / s}" stroke-linejoin="round"`;
  return `<g transform="translate(${x},${y}) scale(${s})">
    <!-- brazo de atrás -->
    <g transform="translate(-30,-120) rotate(${opt.armBack ?? 40})"><rect x="-14" y="0" width="28" height="78" rx="6" fill="${shirt}" ${ol}/><rect x="-14" y="46" width="28" height="32" rx="6" fill="${skin}" ${ol}/></g>
    <!-- pierna de apoyo -->
    <g transform="translate(-14,0) rotate(${opt.standLeg ?? 12})"><rect x="-17" y="0" width="34" height="42" rx="5" fill="${shorts}" ${ol}/><rect x="-15" y="40" width="30" height="48" rx="5" fill="${shirt}" ${ol}/><rect x="-17" y="84" width="46" height="20" rx="7" fill="${boot}" ${ol}/></g>
    <!-- torso -->
    <rect x="-42" y="-130" width="84" height="110" rx="10" fill="${shirt}" ${ol}/>
    <text x="0" y="-55" text-anchor="middle" font-family="Lilita One" font-size="52" fill="#fff" stroke="#14121a" stroke-width="3" paint-order="stroke">${num}</text>
    <rect x="-42" y="-28" width="84" height="30" rx="6" fill="${shorts}" ${ol}/>
    <!-- pierna que patea -->
    <g transform="translate(18,-4) rotate(${opt.kickLeg ?? -75})"><rect x="-17" y="0" width="34" height="42" rx="5" fill="${shorts}" ${ol}/><rect x="-15" y="40" width="30" height="48" rx="5" fill="${shirt}" ${ol}/><rect x="-17" y="84" width="46" height="20" rx="7" fill="${boot}" ${ol}/></g>
    <!-- cabeza -->
    <rect x="-40" y="-222" width="80" height="84" rx="14" fill="${skin}" ${ol}/>
    <rect x="-42" y="-228" width="84" height="26" rx="10" fill="${hair}" ${ol}/>
    <rect x="${opt.crown ? -40 : 0}" y="-262" width="80" height="0" />
    ${opt.crown ? `<path d="M-38,-226 L-38,-262 L-20,-242 L0,-270 L20,-242 L38,-262 L38,-226 Z" fill="#ffc832" ${ol}/>` : ''}
    <rect x="10" y="-190" width="9" height="16" rx="4" fill="#14121a"/><rect x="26" y="-190" width="9" height="16" rx="4" fill="#14121a"/>
    <path d="M6,-162 Q22,-148 38,-164" fill="none" stroke="#14121a" stroke-width="5" stroke-linecap="round"/>
    <!-- brazo de adelante -->
    <g transform="translate(34,-122) rotate(${opt.armFront ?? -60})"><rect x="-14" y="0" width="28" height="78" rx="6" fill="${shirt}" ${ol}/><rect x="-14" y="46" width="28" height="32" rx="6" fill="${skin}" ${ol}/></g>
  </g>`;
}

function clover(x, y, s) {
  let leaves = '';
  for (let i = 0; i < 4; i++) leaves += `<circle cx="${x + Math.cos(i * Math.PI / 2) * 14 * s}" cy="${y + Math.sin(i * Math.PI / 2) * 14 * s}" r="${15 * s}" fill="#3fd45a" stroke="#14121a" stroke-width="${3 * s}"/>`;
  return leaves + `<circle cx="${x}" cy="${y}" r="${8 * s}" fill="#2bb848"/>`;
}

function planet(cx, cy, r, color, ring, broken) {
  const id = 'p' + Math.random().toString(36).slice(2, 7);
  let out = `<defs><radialGradient id="${id}" cx="35%" cy="30%" r="80%"><stop offset="0" stop-color="#fff" stop-opacity="0.5"/><stop offset="0.35" stop-color="${color}"/><stop offset="1" stop-color="#14121a" stop-opacity="0.6"/></radialGradient></defs>`;
  if (broken) {
    // pedazos volando
    for (let i = 0; i < 9; i++) {
      const a = i * 40 * Math.PI / 180, d = r * (0.6 + (i % 3) * 0.35), sz = r * (0.3 + (i % 2) * 0.15);
      out += `<rect x="${cx + Math.cos(a) * d - sz / 2}" y="${cy + Math.sin(a) * d - sz / 2}" width="${sz}" height="${sz}" fill="${color}" stroke="#14121a" stroke-width="3" transform="rotate(${i * 23} ${cx + Math.cos(a) * d} ${cy + Math.sin(a) * d})"/>`;
    }
    out += `<circle cx="${cx}" cy="${cy}" r="${r * 0.9}" fill="#ffe680" opacity="0.85"/><circle cx="${cx}" cy="${cy}" r="${r * 0.55}" fill="#fff" opacity="0.9"/>`;
    return out;
  }
  if (ring) out += `<ellipse cx="${cx}" cy="${cy}" rx="${r * 1.7}" ry="${r * 0.45}" fill="none" stroke="#efe3ff" stroke-width="${r * 0.12}" opacity="0.8" transform="rotate(-18 ${cx} ${cy})"/>`;
  out += `<circle cx="${cx}" cy="${cy}" r="${r}" fill="url(#${id})" stroke="#14121a" stroke-width="4"/>`;
  return out;
}

function stars(w, h, n, seed) {
  let s = seed, out = '';
  const rnd = () => (s = (s * 16807) % 2147483647) / 2147483647;
  for (let i = 0; i < n; i++) out += `<circle cx="${rnd() * w}" cy="${rnd() * h}" r="${1 + rnd() * 3}" fill="#fff" opacity="${0.4 + rnd() * 0.6}"/>`;
  return out;
}
