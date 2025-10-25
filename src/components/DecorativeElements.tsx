import { motion } from 'framer-motion'

interface CardGlowProps {
  className?: string
}

export function CardGlow({ className = '' }: CardGlowProps) {
  return (
    <motion.div
      className={`absolute inset-0 rounded-lg bg-gradient-to-br from-primary/5 via-transparent to-accent/5 opacity-0 group-hover:opacity-100 transition-opacity duration-300 pointer-events-none ${className}`}
      initial={false}
    />
  )
}

interface DecorativeDotsProps {
  className?: string
  count?: number
}

export function DecorativeDots({ className = '', count = 3 }: DecorativeDotsProps) {
  return (
    <div className={`flex gap-1 ${className}`}>
      {Array.from({ length: count }).map((_, i) => (
        <motion.div
          key={i}
          className="w-1 h-1 rounded-full bg-primary/20"
          animate={{
            scale: [1, 1.2, 1],
            opacity: [0.4, 0.8, 0.4],
          }}
          transition={{
            duration: 2,
            repeat: Infinity,
            ease: "easeInOut",
            delay: i * 0.2,
          }}
        />
      ))}
    </div>
  )
}

interface GradientBorderProps {
  className?: string
  children: React.ReactNode
}

export function GradientBorder({ className = '', children }: GradientBorderProps) {
  return (
    <div className={`relative ${className}`}>
      <div className="absolute inset-0 rounded-lg bg-gradient-to-r from-primary/20 via-accent/20 to-secondary/20 blur-sm opacity-0 group-hover:opacity-100 transition-opacity duration-300" />
      <div className="relative">
        {children}
      </div>
    </div>
  )
}

interface AnimatedIconWrapperProps {
  children: React.ReactNode
  className?: string
}

export function AnimatedIconWrapper({ children, className = '' }: AnimatedIconWrapperProps) {
  return (
    <motion.div
      className={className}
      whileHover={{ scale: 1.1, rotate: 5 }}
      transition={{ type: "spring", stiffness: 400, damping: 10 }}
    >
      {children}
    </motion.div>
  )
}

export function CornerAccent({ position = 'top-left' }: { position?: 'top-left' | 'top-right' | 'bottom-left' | 'bottom-right' }) {
  const positionClasses = {
    'top-left': 'top-0 left-0',
    'top-right': 'top-0 right-0 rotate-90',
    'bottom-left': 'bottom-0 left-0 -rotate-90',
    'bottom-right': 'bottom-0 right-0 rotate-180',
  }

  return (
    <svg 
      className={`absolute ${positionClasses[position]} w-8 h-8 opacity-10 pointer-events-none`}
      viewBox="0 0 100 100"
    >
      <path
        d="M 0 0 L 50 0 L 0 50 Z"
        fill="currentColor"
        className="text-primary"
      />
    </svg>
  )
}

interface PulsingDotProps {
  className?: string
  color?: 'primary' | 'accent' | 'secondary'
}

export function PulsingDot({ className = '', color = 'primary' }: PulsingDotProps) {
  const colorClasses = {
    primary: 'bg-primary',
    accent: 'bg-accent',
    secondary: 'bg-secondary',
  }

  return (
    <div className={`relative ${className}`}>
      <motion.div
        className={`w-2 h-2 rounded-full ${colorClasses[color]}`}
        animate={{
          scale: [1, 1.2, 1],
        }}
        transition={{
          duration: 2,
          repeat: Infinity,
          ease: "easeInOut",
        }}
      />
      <motion.div
        className={`absolute inset-0 w-2 h-2 rounded-full ${colorClasses[color]} opacity-50`}
        animate={{
          scale: [1, 2, 1],
          opacity: [0.5, 0, 0.5],
        }}
        transition={{
          duration: 2,
          repeat: Infinity,
          ease: "easeInOut",
        }}
      />
    </div>
  )
}
