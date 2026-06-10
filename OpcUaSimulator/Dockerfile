FROM node:22-alpine
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY simulator.js .
EXPOSE 4840
CMD ["npm", "start"]