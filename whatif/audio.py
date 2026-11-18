"""Procedural sound design for the What-If videos.
Usage: python3 audio.py out/<name>.audio.json out/<name>.wav
Spec: {duration, music:[{t,mood}], amb:[{type,from,to,gain}], sfx:[{t,type,d?,gain?}]}
"""
import json, sys, wave
import numpy as np

SR = 44100
rs = np.random.RandomState(1234)


def noise(n):
    return rs.randn(n).astype(np.float32)


def onepole_lp(x, fc):
    """one-pole lowpass; fc can be scalar or array"""
    fc = np.broadcast_to(np.asarray(fc, dtype=np.float32), x.shape)
    a = np.exp(-2 * np.pi * fc / SR)
    y = np.empty_like(x); s = 0.0
    # vectorised in blocks via scipy-free recursion
    for i in range(len(x)):
        s = (1 - a[i]) * x[i] + a[i] * s
        y[i] = s
    return y


def lp_fft(x, fc, order=2):
    X = np.fft.rfft(x); f = np.fft.rfftfreq(len(x), 1 / SR)
    H = 1 / np.sqrt(1 + (f / fc) ** (2 * order))
    return np.fft.irfft(X * H, len(x)).astype(np.float32)


def bp_fft(x, lo, hi, order=2):
    X = np.fft.rfft(x); f = np.fft.rfftfreq(len(x), 1 / SR)
    H = 1 / np.sqrt(1 + (f / hi) ** (2 * order)) * (1 / np.sqrt(1 + (lo / np.maximum(f, 1e-3)) ** (2 * order)))
    return np.fft.irfft(X * H, len(x)).astype(np.float32)


def env_ad(n, a, d):
    t = np.arange(n) / SR
    e = np.where(t < a, t / max(a, 1e-4), np.exp(-(t - a) / max(d, 1e-4)))
    return e.astype(np.float32)


def fade(n, fin, fout):
    e = np.ones(n, np.float32)
    i = int(fin * SR); o = int(fout * SR)
    if i > 0: e[:i] = np.linspace(0, 1, i)
    if o > 0: e[-o:] *= np.linspace(1, 0, o)
    return e


def norm(x, peak=1.0):
    m = np.max(np.abs(x)) + 1e-9
    return x / m * peak


# ---------------- ambiences ----------------
def amb_waves(n):
    t = np.arange(n) / SR
    base = lp_fft(noise(n), 500) * 0.6
    swell = 0.55 + 0.45 * np.sin(2 * np.pi * t / 6.5) ** 2
    crash = bp_fft(noise(n), 800, 6000) * 0.25 * np.clip(np.sin(2 * np.pi * t / 6.5 - 0.6), 0, 1) ** 3
    return norm(base * swell + crash, 0.5)


def amb_crowd(n):
    t = np.arange(n) / SR
    out = np.zeros(n, np.float32)
    # murmur: many band-limited voices with syllabic modulation
    for k in range(10):
        f0 = rs.uniform(300, 900)
        v = bp_fft(noise(n), f0, f0 * 2.4)
        mod = np.clip(np.sin(2 * np.pi * rs.uniform(2.5, 5.5) * t + rs.uniform(0, 6)), 0, 1)
        mod *= 0.5 + 0.5 * np.sin(2 * np.pi * rs.uniform(0.1, 0.4) * t + rs.uniform(0, 6))
        out += v * mod
    out += lp_fft(noise(n), 400) * 0.4
    return norm(out, 0.45)


def amb_wind(n):
    t = np.arange(n) / SR
    w = bp_fft(noise(n), 200, 1200) * (0.6 + 0.4 * np.sin(2 * np.pi * 0.11 * t) * np.sin(2 * np.pi * 0.047 * t + 1))
    return norm(w, 0.4)


def amb_storm(n):
    t = np.arange(n) / SR
    w = bp_fft(noise(n), 80, 3000) * (0.7 + 0.3 * np.sin(2 * np.pi * 0.7 * t))
    w += bp_fft(noise(n), 1500, 6000) * 0.4 * (0.5 + 0.5 * np.sin(2 * np.pi * 1.3 * t) ** 2)
    return norm(w, 0.7)


def amb_night(n):
    t = np.arange(n) / SR
    out = lp_fft(noise(n), 300) * 0.15
    # crickets
    for k in range(5):
        f = rs.uniform(3800, 5200); rate = rs.uniform(1.2, 2.2); ph = rs.uniform(0, 1)
        gate = ((t * rate + ph) % 1) < 0.18
        chirp = np.sin(2 * np.pi * f * t) * (np.sin(2 * np.pi * 40 * t) > 0) * gate
        out += chirp.astype(np.float32) * 0.05
    return norm(out, 0.3)


def amb_underwater(n):
    t = np.arange(n) / SR
    out = lp_fft(noise(n), 180) * 1.0
    for k in range(40):
        s = int(rs.uniform(0, n - SR // 4)); L = int(SR * 0.08)
        f = rs.uniform(400, 1400)
        tt = np.arange(L) / SR
        out[s:s + L] += np.sin(2 * np.pi * (f + 4000 * tt) * tt) * np.exp(-tt * 40) * 0.3
    return norm(out, 0.6)


def amb_city(n):
    t = np.arange(n) / SR
    out = lp_fft(noise(n), 250) * 0.8 + bp_fft(noise(n), 400, 2000) * 0.15
    return norm(out, 0.4)


AMB = dict(waves=amb_waves, crowd=amb_crowd, wind=amb_wind, storm=amb_storm, night=amb_night, underwater=amb_underwater, city=amb_city)


# ---------------- music beds ----------------
def pad(n, freqs, bright=900, vib=0.15):
    t = np.arange(n) / SR
    out = np.zeros(n, np.float32)
    for f in freqs:
        for det in (-0.12, 0.0, 0.11):
            ff = f * (1 + det / 100)
            ph = rs.uniform(0, 6)
            saw = 2 * ((ff * t + ph) % 1) - 1
            out += saw.astype(np.float32) * 0.3 + np.sin(2 * np.pi * ff * t + ph).astype(np.float32)
    out = lp_fft(out, bright)
    out *= (1 + vib * np.sin(2 * np.pi * 0.2 * t)).astype(np.float32)
    return norm(out, 0.35)


def music_bed(mood, n):
    t = np.arange(n) / SR
    if mood == 'curious':   # light plucky arpeggio over soft pad (D major-ish)
        out = pad(n, [146.8, 220, 293.7, 370], 700) * 0.6
        notes = [587.3, 740, 880, 740, 659.3, 740, 880, 1108.7]
        step = 0.25
        for i in range(int(n / SR / step)):
            f = notes[i % len(notes)]; s = int(i * step * SR); L = min(int(0.6 * SR), n - s)
            if L <= 0: break
            tt = np.arange(L) / SR
            out[s:s + L] += (np.sin(2 * np.pi * f * tt) * np.exp(-tt * 9) * 0.12).astype(np.float32)
        return out
    if mood == 'tense':     # low pulse + heartbeat
        out = pad(n, [73.4, 110, 155.6], 400) * 0.7
        pulse = (np.sin(2 * np.pi * 2.0 * t) > 0.6).astype(np.float32)
        out += lp_fft(noise(n), 120) * pulse * 0.6
        return out
    if mood == 'drone':
        return pad(n, [55, 82.4, 110, 164.8], 500, 0.25) * 0.8
    if mood == 'night':
        return pad(n, [110, 164.8, 207.7, 246.9], 650, 0.2) * 0.6
    if mood == 'cold':
        out = pad(n, [98, 146.8, 233.1, 293.7], 1400, 0.1) * 0.5
        # glassy bells
        for i in range(int(n / SR / 1.5)):
            f = [1174.7, 1396.9, 1760, 1568][i % 4]; s = int(i * 1.5 * SR); L = min(int(2.5 * SR), n - s)
            if L <= 0: break
            tt = np.arange(L) / SR
            out[s:s + L] += (np.sin(2 * np.pi * f * tt) * np.exp(-tt * 2.2) * 0.07).astype(np.float32)
        return out
    if mood == 'epic':
        out = pad(n, [65.4, 98, 130.8, 196, 261.6], 1200, 0.1) * 0.8
        for i in range(int(n / SR / 0.5)):
            s = int(i * 0.5 * SR); L = min(int(0.4 * SR), n - s)
            if L <= 0: break
            tt = np.arange(L) / SR
            out[s:s + L] += (np.sin(2 * np.pi * 50 * tt * np.exp(-tt * 6)) * np.exp(-tt * 9) * (0.5 if i % 2 == 0 else 0.25)).astype(np.float32)
        return out
    return np.zeros(n, np.float32)


# ---------------- sfx ----------------
def sfx(kind, d=None):
    if kind == 'whoosh':
        n = int(0.9 * SR); t = np.arange(n) / SR
        e = np.sin(np.pi * np.clip(t / 0.9, 0, 1)) ** 2
        fc = 300 + 3500 * np.sin(np.pi * t / 0.9)
        x = noise(n); y = np.zeros(n, np.float32)
        # piecewise bandpass via chunks
        for i in range(0, n, 2048):
            c = x[i:i + 2048]; f = fc[min(i + 1024, n - 1)]
            y[i:i + 2048] = bp_fft(c, f * 0.5, f * 1.6)
        return norm(y * e, 0.5)
    if kind == 'whoosh_up':
        n = int(1.4 * SR); t = np.arange(n) / SR
        x = bp_fft(noise(n), 200, 4000) * np.clip(t / 0.15, 0, 1) * np.exp(-t * 2)
        return norm(x, 0.55)
    if kind == 'tick':
        n = int(0.12 * SR); t = np.arange(n) / SR
        return (np.sin(2 * np.pi * 1800 * t) * np.exp(-t * 60) * 0.35 + bp_fft(noise(n), 2000, 8000) * np.exp(-t * 120) * 0.2).astype(np.float32)
    if kind == 'beep':
        n = int(0.35 * SR); t = np.arange(n) / SR
        return (np.sin(2 * np.pi * 880 * t) * env_ad(n, 0.005, 0.12) * 0.4).astype(np.float32)
    if kind == 'riser':
        dd = d or 1.5; n = int(dd * SR); t = np.arange(n) / SR
        f = 200 * (1 + 6 * (t / dd) ** 2)
        tone = np.sin(2 * np.pi * np.cumsum(f) / SR) * 0.2
        nz = bp_fft(noise(n), 1000, 8000) * (t / dd) ** 2 * 0.5
        return norm((tone + nz) * (t / dd), 0.5)
    if kind == 'boom':
        n = int(3.0 * SR); t = np.arange(n) / SR
        f = 30 + 70 * np.exp(-t * 8)
        body = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 1.6)
        crack = lp_fft(noise(n), 1500) * np.exp(-t * 6)
        return norm(np.tanh((body * 1.2 + crack * 0.8) * 2), 0.95)
    if kind == 'thud':
        n = int(0.8 * SR); t = np.arange(n) / SR
        return norm(lp_fft(noise(n), 200) * np.exp(-t * 8) + np.sin(2 * np.pi * 60 * t) * np.exp(-t * 10), 0.8)
    if kind == 'rumble':
        dd = d or 2; n = int(dd * SR); t = np.arange(n) / SR
        return norm(lp_fft(noise(n), 90) * np.sin(np.pi * t / dd), 0.7)
    if kind == 'crowd_cheer':
        n = int(2.2 * SR); t = np.arange(n) / SR
        x = sum(bp_fft(noise(n), f, f * 1.8) * (0.6 + 0.4 * np.sin(2 * np.pi * rs.uniform(3, 7) * t)) for f in (500, 800, 1200, 1700))
        return norm(x * np.clip(t / 0.15, 0, 1) * np.exp(-t * 1.2), 0.5)
    if kind == 'gasp':
        n = int(1.2 * SR); t = np.arange(n) / SR
        x = bp_fft(noise(n), 900, 3500) * np.clip(t / 0.1, 0, 1) * np.exp(-t * 3)
        return norm(x, 0.4)
    if kind == 'pop':
        n = int(0.25 * SR); t = np.arange(n) / SR
        f = 400 + 900 * np.exp(-t * 30)
        return (np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 18) * 0.5).astype(np.float32)
    if kind == 'ding':
        n = int(1.2 * SR); t = np.arange(n) / SR
        return ((np.sin(2 * np.pi * 1318.5 * t) + 0.5 * np.sin(2 * np.pi * 1975.5 * t)) * np.exp(-t * 4) * 0.3).astype(np.float32)
    if kind == 'alarm':  # emergency-alert style two-tone
        dd = d or 2.5; n = int(dd * SR); t = np.arange(n) / SR
        x = (np.sin(2 * np.pi * 853 * t) + np.sin(2 * np.pi * 960 * t)) * 0.25 * ((t % 0.5) < 0.38)
        return x.astype(np.float32)
    if kind == 'power_down':
        dd = d or 1.6; n = int(dd * SR); t = np.arange(n) / SR
        f = 220 * np.exp(-t * 2.5) + 20
        x = np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 1.2)
        x += np.sin(2 * np.pi * np.cumsum(f * 2.01) / SR) * 0.4 * np.exp(-t * 2)
        return norm(x, 0.6)
    if kind == 'shimmer':
        n = int(2.5 * SR); t = np.arange(n) / SR
        x = sum(np.sin(2 * np.pi * f * t + i) * np.exp(-t * (1.5 + i * 0.3)) for i, f in enumerate((1568, 2093, 2637, 3136)))
        return norm(x * np.clip(t / 0.3, 0, 1), 0.25)
    if kind == 'crack':  # ice cracking
        n = int(1.0 * SR); out = np.zeros(n, np.float32)
        for k in range(12):
            s = int(rs.uniform(0, 0.7) * SR); L = int(0.03 * SR)
            out[s:s + L] += bp_fft(noise(L), 1000, 7000) * np.exp(-np.arange(L) / SR * 120)
        out += lp_fft(noise(n), 200) * np.exp(-np.arange(n) / SR * 4) * 0.5
        return norm(out, 0.6)
    if kind == 'heartbeat':
        n = int(1.0 * SR); t = np.arange(n) / SR
        b = lambda o: np.sin(2 * np.pi * 50 * (t - o)) * np.exp(-np.clip(t - o, 0, None) * 18) * (t > o)
        return norm(b(0) + 0.7 * b(0.28), 0.7)
    if kind == 'splash':
        n = int(1.8 * SR); t = np.arange(n) / SR
        return norm(bp_fft(noise(n), 300, 5000) * np.clip(t / 0.05, 0, 1) * np.exp(-t * 2.5), 0.8)
    if kind == 'whistle':  # lifeguard whistle: three blasts
        n = int(1.6 * SR); t = np.arange(n) / SR
        gate = (((t % 0.5) < 0.32) & (t < 1.5)).astype(np.float32)
        f = 2900 + 120 * np.sin(2 * np.pi * 34 * t)
        x = np.sin(2 * np.pi * np.cumsum(f) / SR) * gate + bp_fft(noise(n), 2500, 4000) * 0.15 * gate
        return norm(x, 0.35)
    if kind == 'scream':  # crowd panic
        n = int(2.5 * SR); t = np.arange(n) / SR
        x = sum(bp_fft(noise(n), f, f * 1.5) * (0.5 + 0.5 * np.sin(2 * np.pi * rs.uniform(4, 9) * t + rs.uniform(0, 6))) for f in (700, 1100, 1600, 2300))
        return norm(x * np.clip(t / 0.1, 0, 1) * np.exp(-t * 0.8), 0.55)
    if kind == 'sub_drop':
        n = int(2.0 * SR); t = np.arange(n) / SR
        f = 80 * np.exp(-t * 1.5) + 25
        return norm(np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t * 1.2), 0.9)
    return np.zeros(1, np.float32)


def main(spec_path, out_path):
    spec = json.load(open(spec_path))
    D = spec['duration']; N = int(D * SR)
    mix = np.zeros(N, np.float32)
    duck = np.ones(N, np.float32)

    def add(buf, t, gain=1.0):
        s = int(t * SR)
        if s >= N: return
        L = min(len(buf), N - s); mix[s:s + L] += buf[:L] * gain

    # music segments (crossfaded)
    mus = spec.get('music', [])
    for i, m in enumerate(mus):
        t0 = m['t']; t1 = mus[i + 1]['t'] if i + 1 < len(mus) else D
        if m['mood'] == 'none' or t1 <= t0: continue
        n = int((t1 - t0 + 1.0) * SR)
        b = music_bed(m['mood'], n) * fade(n, 0.6, 1.0) * m.get('gain', 0.55)
        add(b, max(0, t0 - 0.3))
    for a in spec.get('amb', []):
        n = int((a['to'] - a['from']) * SR)
        if n <= 0: continue
        b = AMB[a['type']](n) * fade(n, a.get('fin', 1.0), a.get('fout', 1.0)) * a.get('gain', 0.5)
        add(b, a['from'])
    for s in spec.get('sfx', []):
        if s['type'] == 'mute':
            a = int(s['t'] * SR); b = int((s['t'] + s.get('d', 0.3)) * SR)
            duck[a:b] = 0.0
            continue
        add(sfx(s['type'], s.get('d')), s['t'], s.get('gain', 1.0))
    # smooth duck
    k = int(0.01 * SR); duck = np.convolve(duck, np.ones(k) / k, mode='same')
    mix *= duck
    # master: soft clip + normalize to ~ -1 dBFS
    rms = np.sqrt(np.mean(mix[: int((D - 5) * SR)] ** 2)) + 1e-9
    mix = mix * (0.16 / rms)                 # target ~ -18 dBFS RMS
    mix = np.tanh(mix * 1.3) / 1.3 * 1.15     # soft limiter
    pk = np.max(np.abs(mix))
    if pk > 0.95: mix = mix / pk * 0.95
    st = np.stack([mix, mix], 1)
    data = (st * 32767).astype(np.int16)
    with wave.open(out_path, 'wb') as w:
        w.setnchannels(2); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())
    print('audio', out_path, D, 's')


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
