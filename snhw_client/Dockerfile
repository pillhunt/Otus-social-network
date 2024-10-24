# 1. Создаем приложение Angular
#FROM node:alpine as builder
#
#WORKDIR /client
#COPY ./snhw_client/package.json ./snhw_client/package-lock.json .
#ENV CI=1
#RUN npm i
#
#COPY ./snhw_client .
#RUN npm run build 
##-- --prod --output-path=/dist

# 2. Развертываем приложение Angular на NGINX
#FROM nginx:alpine

# Заменяем дефолтную страницу nginx соответствующей веб-приложению
#RUN rm -rf /usr/share/nginx/html/*
#COPY /snhw_client/index.html /usr/share/nginx/html
#COPY --from=builder /snhw_client/src/ /usr/share/nginx/html/src/

#происходит монтирование на образе
#COPY ./nginx/nginx.conf /etc/nginx/nginx.conf

#ENTRYPOINT ["nginx", "-g", "daemon off;"]

FROM ubuntu

ENV DEBIAN_FRONTEND noninteractive

RUN apt-get update && apt-get install -y nodejs npm && rm -rf /var/lib/apt/lists/*
RUN npm install -g yarn

WORKDIR /srv/snhw_client
COPY ./package*.json ./
RUN yarn global add @vue/cli
RUN yarn install

# Bundle app source
COPY . .

EXPOSE 8082

#CMD [ "yarn", "serve"]

