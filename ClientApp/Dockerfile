FROM node:12-alpine as builder
WORKDIR /app
COPY ./planning_poker/package.json ./
COPY ./planning_poker/yarn.lock ./
RUN yarn install

COPY ./planning_poker/ ./

WORKDIR /app/planning_poker
RUN yarn lint && yarn build

FROM nginx:latest
COPY ./nginx.conf /etc/nginx/nginx.conf
COPY --from=builder /app/build /usr/share/nginx/html
EXPOSE 80