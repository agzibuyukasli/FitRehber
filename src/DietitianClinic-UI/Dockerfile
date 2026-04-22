# ─────────────────────────────────────────
#  Stage 1 – bağımlılıkları kur
# ─────────────────────────────────────────
FROM node:18-alpine AS deps
WORKDIR /app

# Yalnızca package dosyalarını kopyala → layer cache optimizasyonu
COPY package.json package-lock.json* ./
RUN npm ci

# ─────────────────────────────────────────
#  Stage 2 – build
# ─────────────────────────────────────────
FROM node:18-alpine AS builder
WORKDIR /app

# Önce node_modules, sonra kaynak kodu (cache'den faydalanmak için sıra önemli)
COPY --from=deps /app/node_modules ./node_modules
COPY . .

ENV NEXT_TELEMETRY_DISABLED=1

RUN npm run build

# ─────────────────────────────────────────
#  Stage 3 – production runtime
# ─────────────────────────────────────────
FROM node:18-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# Non-root kullanıcı
RUN addgroup --system --gid 1001 nodejs \
 && adduser  --system --uid 1001 nextjs

COPY --from=builder /app/public           ./public
COPY --from=builder /app/.next            ./.next
COPY --from=builder /app/node_modules     ./node_modules
COPY --from=builder /app/package.json     ./package.json

USER nextjs
EXPOSE 3000
ENV PORT=3000

CMD ["npm", "start"]
