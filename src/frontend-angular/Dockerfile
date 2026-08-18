# Stage 1: Build Angular App
FROM node:22-alpine AS build
WORKDIR /app

ARG CONFIGURATION=production

COPY package*.json ./
RUN npm install

COPY . .
RUN npm run build:${CONFIGURATION} || npm run build

# Stage 2: Serve static Angular build with Nginx Alpine
FROM nginx:alpine AS final
COPY --from=build /app/dist/app/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
