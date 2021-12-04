#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY *.sln ./
COPY IntegrationTests/IntegrationTests.fsproj ./IntegrationTests/
COPY PlanningPoker/PlanningPoker.fsproj ./PlanningPoker/
COPY Tests/Tests.fsproj ./Tests/
COPY Api/Api.fsproj ./Api/
COPY Gateway/Gateway.fsproj ./Gateway/
COPY GrainInterfaces/GrainInterfaces.csproj ./GrainInterfaces/
COPY Grains/Grains.csproj ./Grains/
RUN dotnet restore
COPY . .
RUN dotnet build

FROM build as Tests
WORKDIR "/src/Tests"
ENTRYPOINT ["dotnet", "test", "--logger:trx"]

FROM build as IntegrationTests
WORKDIR "/src/IntegrationTests"
ENTRYPOINT ["dotnet", "test", "--logger:trx"]


FROM build AS publish
RUN dotnet test --no-build --verbosity normal
WORKDIR "/src/Api"
RUN dotnet publish "Api.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "Api.dll"]