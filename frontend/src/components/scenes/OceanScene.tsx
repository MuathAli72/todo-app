import { useState } from 'react'

/**
 * Ocean world: a crescent moon over the sea, with an island.
 *
 * @remarks
 * Hover the moon: a soft glow. Click it: a meteor. Hover the sea: waves move. Click it: a boat sails.
 * Hover the island: the palm sways. Hover effects are CSS (scenes.css); clicks keep state here.
 */
export function OceanScene() {
  // Changing the key re-creates the meteor, which restarts its animation.
  const [meteors, setMeteors] = useState(0)
  const [boatOut, setBoatOut] = useState(false)

  return (
    <svg className="scene" viewBox="0 0 320 160" role="img" aria-label="A crescent moon over a calm sea with a small island">
      <rect width="320" height="160" fill="#0B2A3C" />
      <g fill="#EAFBFF">
        <circle cx="130" cy="20" r="1.5" /><circle cx="240" cy="30" r="1.5" /><circle cx="290" cy="58" r="1" />
        <circle cx="190" cy="14" r="1" /><circle cx="25" cy="80" r="1" /><circle cx="160" cy="48" r="1" />
      </g>
      {meteors > 0 && (
        <g key={meteors} className="ocean-meteor">
          <line x1="290" y1="12" x2="308" y2="5" stroke="#EAFBFF" strokeWidth="2" strokeLinecap="round" />
        </g>
      )}
      <g className="ocean-moon clickable" onClick={() => setMeteors(meteors + 1)}>
        <g className="ocean-glow">
          <circle cx="60" cy="42" r="34" fill="#EAFBFF" opacity="0.05" />
          <circle cx="60" cy="42" r="27" fill="#EAFBFF" opacity="0.07" />
          <circle cx="60" cy="42" r="21" fill="#EAFBFF" opacity="0.1" />
        </g>
        <circle cx="60" cy="42" r="16" fill="#EAFBFF" />
        <circle cx="67" cy="37" r="14" fill="#0B2A3C" />
      </g>
      <g className="ocean-sea clickable" onClick={() => setBoatOut(!boatOut)}>
        <path className="ocean-wave" d="M-20 106 Q60 98 140 106 T300 104 T360 106 V160 H-20 Z" fill="#11607A" />
        <g className={boatOut ? 'ocean-boat out' : 'ocean-boat'}>
          <path d="M10 114 h24 l-4 6 h-16 z" fill="#FFD166" />
          <rect x="21" y="100" width="1.5" height="14" fill="#EAFBFF" />
          <path d="M22.5 101 l10 11 h-10 z" fill="#EAFBFF" />
        </g>
        <path className="ocean-wave ocean-wave-back" d="M-20 124 Q60 116 140 124 T300 122 T360 124 V160 H-20 Z" fill="#14889E" />
      </g>
      <g className="ocean-island">
        <path d="M200 106 Q228 74 256 106 Z" fill="#3DDC97" />
        <g className="ocean-palm">
          <rect x="226" y="82" width="3" height="18" fill="#7A4E2D" />
          <circle cx="227.5" cy="80" r="9" fill="#2FBF80" />
        </g>
      </g>
    </svg>
  )
}
