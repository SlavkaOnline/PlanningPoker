FROM node:12-alpine as builder
WORKDIR /app
COPY ./planning_poker/ ./

WORKDIR /app/planning_poker
RUN yarn install && yarn lint && yarn build

FROM nginx:latest
COPY ./Docker/Nginx/nginx.conf /etc/nginx/nginx.conf
COPY --from=builder /app/planning_poker/build /usr/share/nginx/html
EXPOSE 80