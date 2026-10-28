import { useState } from 'react'

/** Sky colors for dawn, noon and dusk. Clicking the sun moves to the next one. */
const skyColors = ['#FFE8D6', '#BEE3F8', '#F7B9A0']

/**
 * Desert dawn (the light theme): a sun over sand dunes, with a cactus.
 *
 * @remarks
 * Hover the sun: its rays turn. Click it: dawn, noon, dusk. Hover the cactus: it wiggles.
 * Click it: it blooms.
 */
export function DesertScene() {
  const [skyIndex, setSkyIndex] = useState(0)
  const [bloomed, setBloomed] = useState(false)

  return (
    <svg className="scene" viewBox="0 0 320 160" role="img" aria-label="A morning sun over sand dunes with a cactus">
      <rect className="desert-sky" width="320" height="160" fill={skyColors[skyIndex]} />
      <g className="desert-sun clickable" onClick={() => setSkyIndex((skyIndex + 1) % skyColors.length)}>
        <g className="desert-rays" stroke="#FFB37E" strokeWidth="3" strokeLinecap="round">
          <line x1="85" y1="34" x2="85" y2="24" /><line x1="85" y1="116" x2="85" y2="106" />
          <line x1="44" y1="75" x2="54" y2="75" /><line x1="116" y1="75" x2="126" y2="75" />
          <line x1="56" y1="46" x2="63" y2="53" /><line x1="107" y1="97" x2="114" y2="104" />
          <line x1="56" y1="104" x2="63" y2="97" /><line x1="107" y1="53" x2="114" y2="46" />
        </g>
        <circle cx="85" cy="75" r="26" fill="#FF9F68" />
      </g>
      <path d="M0 106 Q64 82 138 104 T320 96 V160 H0 Z" fill="#F2C38B" />
      <path d="M0 128 Q96 108 180 126 T320 121 V160 H0 Z" fill="#E3A35F" />
      <g className="desert-cactus clickable" onClick={() => setBloomed(!bloomed)}>
        <ellipse cx="252" cy="124" rx="16" ry="3" fill="#CF8E4D" />
        <rect x="245" y="80" width="14" height="45" rx="7" fill="#5E8F3E" />
        <path d="M245 104 h-6 a5 5 0 0 1 -5 -5 v-11 a4 4 0 0 1 8 0 v8 h3" fill="#5E8F3E" />
        <path d="M259 98 h6 a5 5 0 0 0 5 -5 v-13 a4 4 0 0 0 -8 0 v10 h-3" fill="#5E8F3E" />
        <g stroke="#80B85C" strokeWidth="1.2" fill="none">
          <line x1="249" y1="84" x2="249" y2="122" /><line x1="252" y1="82" x2="252" y2="122" />
          <line x1="255" y1="84" x2="255" y2="122" /><line x1="238" y1="88" x2="238" y2="99" /><line x1="266" y1="80" x2="266" y2="93" />
        </g>
        <g fill="#F4E9D0">
          <circle cx="247" cy="90" r="0.7" /><circle cx="257" cy="96" r="0.7" /><circle cx="247" cy="108" r="0.7" />
          <circle cx="257" cy="114" r="0.7" /><circle cx="236" cy="93" r="0.7" /><circle cx="268" cy="86" r="0.7" />
        </g>
        <g className={bloomed ? 'desert-bloom open' : 'desert-bloom'}>
          <circle cx="252" cy="78" r="4.5" fill="#FF6FA3" />
          <circle cx="248.5" cy="80" r="2.5" fill="#FF8DB8" /><circle cx="255.5" cy="80" r="2.5" fill="#FF8DB8" />
          <circle cx="252" cy="78" r="1.6" fill="#FFE066" />
        </g>
      </g>
    </svg>
  )
}
