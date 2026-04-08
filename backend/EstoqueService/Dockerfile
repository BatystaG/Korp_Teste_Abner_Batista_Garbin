# Estágio 1: build — usa a imagem SDK completa para compilar
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /out

# Estágio 2: runtime — imagem menor, só o necessário para rodar
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .

ENTRYPOINT ["dotnet", "EstoqueService.dll"]
