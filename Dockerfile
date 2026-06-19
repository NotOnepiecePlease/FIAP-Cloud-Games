FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG PROJECT
WORKDIR /src
COPY . .
RUN dotnet publish ${PROJECT}/${PROJECT}.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ARG PROJECT
ENV DLL_NAME=${PROJECT}.dll
ENTRYPOINT ["sh", "-c", "dotnet $DLL_NAME"]
