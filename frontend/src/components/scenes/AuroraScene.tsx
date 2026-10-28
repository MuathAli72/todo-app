import { useState } from 'react'

/** The aurora's color pairs. Clicking the sky moves to the next one. */
const auroraColors: [string, string][] = [
  ['#3DFFB0', '#FF7AD9'],
  ['#7AB8FF', '#3DFFB0'],
  ['#FF7AD9', '#B98CFF'],
]

/** Snowy pine trees on the hills: x, y and size. */
const pines: [number, number, number][] = [
  [30, 118, 1], [52, 114, 0.8], [120, 120, 0.7], [230, 112, 1.1], [256, 116, 0.85], [285, 110, 1], [300, 114, 0.75],
]

/** Where the snowflakes start, and how long each one waits (seconds) before falling. */
const flakes: [number, number][] = [[30, 0], [80, 0.8], [110, 2.8], [140, 1.6], [170, 2.4], [200, 0.4], [260, 1.2], [300, 2]]

/**
 * Arctic aurora: green and pink lights over snowy hills and pines.
 *
 * @remarks
 * Hover the sky: the aurora ripples. Click it: new colors. Hover the snow: it sparkles.
 * Click the snow: snowfall on or off.
 */
export function AuroraScene() {
  const [colorIndex, setColorIndex] = useState(0)
  const [snowing, setSnowing] = useState(false)
  const [first, second] = auroraColors[colorIndex]

  return (
    <svg className="scene" viewBox="0 0 320 160" role="img" aria-label="Northern lights over snowy hills with pine trees">
      <rect width="320" height="160" fill="#0A1633" />
      <g className="aurora-sky clickable" onClick={() => setColorIndex((colorIndex + 1) % auroraColors.length)}>
        <rect width="320" height="95" fill="transparent" />
        <path className="aurora-band" d="M0 52 Q64 22 128 47 T256 37 T320 42 V62 Q256 57 192 67 T64 62 T0 72 Z" fill={first} opacity="0.55" />
        <path className="aurora-band aurora-band-back" d="M0 36 Q85 10 170 31 T320 21 V36 Q235 41 160 46 T0 51 Z" fill={second} opacity="0.4" />
        <g fill="#FFFFFF">
          <circle cx="40" cy="14" r="1.5" /><circle cx="210" cy="10" r="1" /><circle cx="300" cy="20" r="1" /><circle cx="110" cy="80" r="1" />
        </g>
      </g>
      <g className={snowing ? 'aurora-snow snowing' : 'aurora-snow'} fill="#FFFFFF">
        {flakes.map(([x, delay]) => (
          <circle key={x} className="aurora-flake" cx={x} cy="0" r="1.4" style={{ animationDelay: `${delay}s` }} />
        ))}
      </g>
      <g className="aurora-hills clickable" onClick={() => setSnowing(!snowing)}>
        <path d="M0 114 Q75 90 150 112 T320 104 V160 H0 Z" fill="#C9DDF5" />
        {pines.map(([x, y, size]) => (
          <g key={x} transform={`translate(${x} ${y}) scale(${size})`}>
            <rect x="-1.5" y="0" width="3" height="5" fill="#5A4030" />
            <path d="M0 -26 L10 -8 H-10 Z M0 -18 L12 2 H-12 Z" fill="#1F5B4A" />
            <path d="M0 -26 L4 -19 Q0 -17 -4 -19 Z M0 -18 L5 -10 Q0 -8 -5 -10 Z M-12 2 L-7 -4 Q-9 0 -4 0 Z" fill="#F4F9FF" />
          </g>
        ))}
        <path d="M0 134 Q95 118 190 132 T320 128 V160 H0 Z" fill="#EAF3FF" />
        <g fill="#FFFFFF">
          <circle className="aurora-sparkle" cx="60" cy="140" r="1.5" /><circle className="aurora-sparkle" cx="120" cy="146" r="1.5" />
          <circle className="aurora-sparkle" cx="190" cy="138" r="1.5" /><circle className="aurora-sparkle" cx="250" cy="146" r="1.5" />
          <circle className="aurora-sparkle" cx="295" cy="136" r="1.5" />
        </g>
      </g>
    </svg>
  )
}
