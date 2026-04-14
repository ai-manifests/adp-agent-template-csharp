# Multi-stage .NET 10 build for an ADP agent based on Adp.Agent.
# Stage 1: build + publish the self-contained output
# Stage 2: minimal runtime image

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY nuget.config ./
COPY MyAdpAgent.csproj ./
RUN dotnet restore MyAdpAgent.csproj

COPY . .
RUN dotnet publish MyAdpAgent.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Non-root user for defense in depth.
RUN useradd -u 10001 -m adpagent
USER 10001

COPY --from=build /app/publish ./
COPY --chown=10001:10001 agents ./agents

ENV ASPNETCORE_URLS=http://+:3000
EXPOSE 3000

ENTRYPOINT ["dotnet", "MyAdpAgent.dll", "agents/example.json"]
