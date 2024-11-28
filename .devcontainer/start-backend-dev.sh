echo "Starting backend"
cd app/backend
dotnet restore
dotnet run --urls=http://localhost:8000/