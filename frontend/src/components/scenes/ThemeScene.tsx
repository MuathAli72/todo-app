import type { ThemeName } from '../../hooks/useTheme'
import { OceanScene } from './OceanScene'
import { AuroraScene } from './AuroraScene'
import { DesertScene } from './DesertScene'
import './scenes.css'

/** Shows the illustrated scene that belongs to the current theme. */
export function ThemeScene({ theme }: { theme: ThemeName }) {
  if (theme === 'aurora') {
    return <AuroraScene />
  }
  if (theme === 'desert') {
    return <DesertScene />
  }
  return <OceanScene />
}
