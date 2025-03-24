# Étape 1 : Builder image (pour construire l'application)
FROM node:18 AS builder

# Définir le répertoire de travail
WORKDIR /app

# Copier les fichiers de définition des dépendances
COPY package*.json ./

# Installer les dépendances
RUN npm install

# Copier le reste des fichiers de l'application
COPY . .

# Définir les variables d'environnement
ARG VITE_BASE_URL
ENV VITE_BASE_URL=${VITE_BASE_URL}

# Construire l'application Nuxt
RUN npm run build

# Étape 2 : Runner image (pour exécuter l'application)
FROM node:18-alpine

# Définir le répertoire de travail
WORKDIR /app

# Copier les fichiers construits de l'étape précédente
COPY --from=builder /app/.output .

# Exposer le port (par défaut : 3000)
EXPOSE 3000

# Commande pour démarrer l'application
CMD ["node", "server/index.mjs"]


