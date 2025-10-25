import { motion } from 'framer-motion'

export function FloatingShapes() {
  return (
    <div className="absolute inset-0 overflow-hidden pointer-events-none">
      <motion.div
        className="absolute top-32 left-20 w-20 h-20 border-2 border-primary/10 rounded-lg"
        animate={{
          y: [0, -30, 0],
          rotate: [0, 10, 0],
        }}
        transition={{
          duration: 8,
          repeat: Infinity,
          ease: "easeInOut",
        }}
      />

      <motion.div
        className="absolute top-1/4 right-32 w-16 h-16 border-2 border-accent/10 rounded-full"
        animate={{
          y: [0, 40, 0],
          scale: [1, 1.1, 1],
        }}
        transition={{
          duration: 10,
          repeat: Infinity,
          ease: "easeInOut",
          delay: 1,
        }}
      />

      <motion.div
        className="absolute bottom-40 left-1/4 w-12 h-12 bg-secondary/5 rounded-full backdrop-blur-sm"
        animate={{
          y: [0, -25, 0],
          opacity: [0.3, 0.6, 0.3],
        }}
        transition={{
          duration: 6,
          repeat: Infinity,
          ease: "easeInOut",
          delay: 0.5,
        }}
      />

      <motion.div
        className="absolute top-2/3 right-1/4 w-24 h-24 border border-primary/8"
        style={{ transform: 'rotate(45deg)' }}
        animate={{
          y: [0, -20, 0],
          rotate: [45, 55, 45],
        }}
        transition={{
          duration: 12,
          repeat: Infinity,
          ease: "easeInOut",
          delay: 2,
        }}
      />

      <svg className="absolute top-1/2 left-1/3 w-20 h-20 opacity-5" viewBox="0 0 100 100">
        <motion.path
          d="M50 10 L90 90 L10 90 Z"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          className="text-accent"
          animate={{
            rotate: [0, 360],
          }}
          transition={{
            duration: 18,
            repeat: Infinity,
            ease: "linear",
          }}
        />
      </svg>

      <motion.div
        className="absolute bottom-1/4 right-1/5 w-8 h-8 border-2 border-secondary/10 rounded-full"
        animate={{
          y: [0, 20, 0],
          x: [0, -10, 0],
        }}
        transition={{
          duration: 9,
          repeat: Infinity,
          ease: "easeInOut",
          delay: 1.5,
        }}
      />

      <svg className="absolute top-1/4 left-1/2 w-16 h-16 opacity-5" viewBox="0 0 100 100">
        <motion.line
          x1="10"
          y1="50"
          x2="90"
          y2="50"
          stroke="currentColor"
          strokeWidth="2"
          className="text-primary"
          animate={{
            rotate: [0, 90, 0],
          }}
          transition={{
            duration: 14,
            repeat: Infinity,
            ease: "easeInOut",
          }}
        />
        <motion.line
          x1="50"
          y1="10"
          x2="50"
          y2="90"
          stroke="currentColor"
          strokeWidth="2"
          className="text-primary"
          animate={{
            rotate: [0, 90, 0],
          }}
          transition={{
            duration: 14,
            repeat: Infinity,
            ease: "easeInOut",
          }}
        />
      </svg>

      <motion.div
        className="absolute top-1/3 right-1/6 w-6 h-6 bg-gradient-to-br from-primary/10 to-accent/10 rounded"
        animate={{
          rotate: [0, 180, 0],
          scale: [1, 1.2, 1],
        }}
        transition={{
          duration: 11,
          repeat: Infinity,
          ease: "easeInOut",
          delay: 0.8,
        }}
      />
    </div>
  )
}
