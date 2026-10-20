# syntax=docker/dockerfile:1
FROM debian:bookworm-slim AS dotnet-sdk
ARG DOTNET_SDK_VERSION=10.0.402
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates curl libicu72 libssl3 zlib1g libstdc++6 && rm -rf /var/lib/apt/lists/*
ENV DOTNET_ROOT=/usr/share/dotnet PATH=/usr/share/dotnet:$PATH DOTNET_CLI_TELEMETRY_OPTOUT=1
RUN --mount=type=secret,id=proxy_ca \
    if [ -f /run/secrets/proxy_ca ]; then export CURL_CA_BUNDLE=/run/secrets/proxy_ca; fi; \
    curl -fsSL "https://builds.dotnet.microsoft.com/dotnet/Sdk/${DOTNET_SDK_VERSION}/dotnet-sdk-${DOTNET_SDK_VERSION}-linux-x64.tar.gz" -o /tmp/sdk.tar.gz && \
    mkdir -p /usr/share/dotnet && tar -xzf /tmp/sdk.tar.gz -C /usr/share/dotnet && rm /tmp/sdk.tar.gz
FROM dotnet-sdk AS build
ARG PROJECT=ChatService.Api
WORKDIR /source
COPY . .
RUN --mount=type=secret,id=proxy_ca \
    if [ -f /run/secrets/proxy_ca ]; then export SSL_CERT_FILE=/run/secrets/proxy_ca; fi; \
    dotnet publish src/${PROJECT}/${PROJECT}.csproj -c Release -r linux-x64 --self-contained true -o /app
FROM debian:bookworm-slim AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates libicu72 libssl3 zlib1g libstdc++6 && rm -rf /var/lib/apt/lists/*
ARG PROJECT=ChatService.Api
ENV APP_BINARY=${PROJECT} ASPNETCORE_URLS=http://+:8080
WORKDIR /app
COPY --from=build --chown=1654:1654 /app .
RUN mkdir -p /app/uploads && chown 1654:1654 /app/uploads
USER 1654:1654
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec \"./$APP_BINARY\""]
