# --build-arg log_path="path_to_log_file" --build-arg sender_mail="sender@mail" --build-arg sender_pass="email_pass" --build-arg target_mail="target@mail" --build-arg url="url"
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
# FROM mcr.microsoft.com/dotnet/aspnet:7.0
FROM alpine:latest

RUN mkdir /app

# install geckodriver
WORKDIR /geckodriver
RUN wget https://github.com/mozilla/geckodriver/releases/download/v0.32.2/geckodriver-v0.32.2-linux64.tar.gz
RUN tar -xvf geckodriver-v0.32.2-linux64.tar.gz
RUN mv geckodriver /app

RUN apk update
RUN apk add aspnetcore7-runtime
RUN apk add firefox
RUN apk add bash

WORKDIR /app

COPY --from=build-env /app/out .

ARG sender_mail
ENV SENDER_MAIL=$sender_mail
ARG log_path
ENV LOG_PATH=$log_path
ARG sender_pass
ENV SENDER_PASS=$sender_pass
ARG target_mail
ENV TARGET_MAIL=$target_mail
ARG url
ENV URL=$url
ARG cinema_name
ARG cinema_town_name
ENV CINEMA_NAME=$cinema_name
ENV CINEMA_TOWN_NAME=$cinema_town_name
ARG timeout
ENV TIMEOUT=$timeout

CMD dotnet AutomaticScheduler.Console.dll -f ${LOG_PATH} -c true -e ${SENDER_MAIL} -p ${SENDER_PASS} -t ${TARGET_MAIL} -u ${URL} -i 30 -v "Verbose" -D 0 --cc-cinema-name ${CINEMA_NAME} --cc-cinema-town-name ${CINEMA_TOWN_NAME} --cc-cinema-vip true --timeout ${TIMEOUT}
