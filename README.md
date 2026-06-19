# FIAP Cloud Games

Projeto acadêmico de referência para a plataforma fictícia **FIAP Cloud Games**, desenvolvido com arquitetura de microserviços, APIs REST, banco de dados local por serviço, mensageria com RabbitMQ, Docker, Swagger/OpenAPI e manifests Kubernetes.

O objetivo é demonstrar uma solução simples, funcional e fácil de executar para cadastro de usuários, autenticação, catálogo de jogos, pagamentos e notificações.

---

## Sumário

- [Visão geral](#visão-geral)
- [Arquitetura](#arquitetura)
- [Serviços](#serviços)
- [Tecnologias utilizadas](#tecnologias-utilizadas)
- [Pré-requisitos](#pré-requisitos)
- [Como clonar o projeto](#como-clonar-o-projeto)
- [Como executar com Docker Compose - recomendado](#como-executar-com-docker-compose---recomendado)
- [Como executar localmente com .NET CLI](#como-executar-localmente-com-net-cli)
- [URLs dos serviços](#urls-dos-serviços)
- [Swagger](#swagger)
- [Fluxo de teste sugerido para o professor](#fluxo-de-teste-sugerido-para-o-professor)
- [Endpoints disponíveis](#endpoints-disponíveis)
- [Exemplos de requisições](#exemplos-de-requisições)
- [RabbitMQ](#rabbitmq)
- [Banco de dados SQLite](#banco-de-dados-sqlite)
- [Kubernetes](#kubernetes)
- [Estrutura de pastas](#estrutura-de-pastas)
- [Comandos úteis](#comandos-úteis)
- [Solução de problemas](#solução-de-problemas)

---

## Visão geral

A solução foi construída como um conjunto de APIs independentes. Cada serviço possui sua própria responsabilidade e seu próprio banco SQLite, evitando acoplamento direto entre domínios.

Principais capacidades:

- Cadastro e autenticação de usuários com JWT.
- Consulta e manutenção de catálogo de jogos.
- Controle simples de estoque.
- Criação e consulta de pagamentos.
- Publicação de eventos no RabbitMQ por serviços de catálogo e pagamento.
- Criação, consulta e leitura de notificações.
- Documentação interativa via Swagger.
- Execução via Docker Compose.
- Manifests básicos para Kubernetes.

---

## Arquitetura

```text
+-------------------+        +-------------------+
|  fcg-users-api    |        | fcg-catalog-api   |
|  Usuários / JWT   |        | Jogos / Estoque   |
|  SQLite próprio   |        | SQLite próprio    |
+-------------------+        +---------+---------+
                                      |
                                      | publica eventos
                                      v
                                +-------------+
                                |  RabbitMQ   |
                                +-------------+
                                      ^
                                      | publica eventos
+-------------------+        +---------+---------+
|fcg-notifications  |        | fcg-payments-api |
|Notificações       |        | Pagamentos       |
|SQLite próprio     |        | SQLite próprio   |
+-------------------+        +------------------+
```

Cada API é uma aplicação .NET 8 Minimal API. Os serviços foram separados para representar domínios diferentes da plataforma.

---

## Serviços

| Serviço | Responsabilidade | Porta no Docker Compose |
|---|---|---:|
| `fcg-users-api` | Cadastro, login e geração de JWT | `5001` |
| `fcg-catalog-api` | Catálogo de jogos e estoque | `5002` |
| `fcg-payments-api` | Pagamentos simplificados | `5003` |
| `fcg-notifications-api` | Notificações de usuários | `5004` |
| `rabbitmq` | Broker de mensageria | `5672` / `15672` |

---

## Tecnologias utilizadas

- **.NET 8**
- **ASP.NET Core Minimal APIs**
- **Entity Framework Core**
- **SQLite**
- **JWT Bearer Authentication**
- **RabbitMQ**
- **Swagger / OpenAPI**
- **Docker**
- **Docker Compose**
- **Kubernetes manifests**

---

## Pré-requisitos

Para executar com Docker Compose, é necessário:

- Git
- Docker Desktop ou Docker Engine com Docker Compose

Para executar localmente sem Docker, é necessário:

- Git
- .NET SDK 8.0 ou superior
- Opcional: RabbitMQ local ou RabbitMQ via Docker

Verifique as instalações:

```bash
git --version
docker --version
docker compose version
dotnet --version
```

> Observação: para a execução via Docker Compose, o .NET SDK não precisa estar instalado na máquina, pois o build ocorre dentro dos containers.

---

## Como clonar o projeto

```bash
git clone <URL_DO_REPOSITORIO>
cd fiap-cloud-games
```

Exemplo:

```bash
git clone https://github.com/<seu-usuario>/<seu-repositorio>.git
cd fiap-cloud-games
```

---

## Como executar com Docker Compose - recomendado

Esta é a forma mais simples para o professor testar todo o projeto.

```bash
cd fiap-cloud-games
docker compose up --build
```

Esse comando irá baixar as imagens base necessárias, compilar as APIs .NET, iniciar o RabbitMQ e iniciar as quatro APIs do projeto.

Em outro terminal, confira os containers:

```bash
docker compose ps
```

Após a inicialização, acesse:

| Serviço | URL |
|---|---|
| Users API | http://localhost:5001/swagger |
| Catalog API | http://localhost:5002/swagger |
| Payments API | http://localhost:5003/swagger |
| Notifications API | http://localhost:5004/swagger |
| RabbitMQ Management | http://localhost:15672 |

Credenciais do RabbitMQ Management:

```text
Usuário: guest
Senha: guest
```

Para parar a aplicação:

```bash
docker compose down
```

---

## Como executar localmente com .NET CLI

```bash
dotnet restore
dotnet build FIAPCloudGames.sln
```

Inicie o RabbitMQ local com Docker:

```bash
docker run -d --name fcg-rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

Se o container já existir:

```bash
docker start fcg-rabbitmq
```

Execute cada API em um terminal separado:

```bash
dotnet run --project ./fcg-users-api/fcg-users-api.csproj --urls http://localhost:5001
dotnet run --project ./fcg-catalog-api/fcg-catalog-api.csproj --urls http://localhost:5002
dotnet run --project ./fcg-payments-api/fcg-payments-api.csproj --urls http://localhost:5003
dotnet run --project ./fcg-notifications-api/fcg-notifications-api.csproj --urls http://localhost:5004
```

---

## URLs dos serviços

Executando via Docker Compose:

| Serviço | Health check | Swagger |
|---|---|---|
| Users API | http://localhost:5001/health | http://localhost:5001/swagger |
| Catalog API | http://localhost:5002/health | http://localhost:5002/swagger |
| Payments API | http://localhost:5003/health | http://localhost:5003/swagger |
| Notifications API | http://localhost:5004/health | http://localhost:5004/swagger |

---

## Swagger

Cada API possui documentação Swagger própria. Para testar pelo navegador:

1. Suba o projeto com `docker compose up --build`.
2. Abra uma das URLs `/swagger` listadas acima.
3. Escolha o endpoint.
4. Clique em **Try it out**.
5. Preencha o JSON de entrada, quando houver.
6. Clique em **Execute**.

---

## Fluxo de teste sugerido para o professor

### 1. Verificar saúde dos serviços

```bash
curl http://localhost:5001/health
curl http://localhost:5002/health
curl http://localhost:5003/health
curl http://localhost:5004/health
```

### 2. Cadastrar um usuário

```bash
curl -X POST http://localhost:5001/users/register \
  -H "Content-Type: application/json" \
  -d '{"name":"Professor FIAP","email":"professor@fiap.com.br","password":"123456"}'
```

### 3. Fazer login

```bash
curl -X POST http://localhost:5001/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"professor@fiap.com.br","password":"123456"}'
```

A resposta retorna um token JWT:

```json
{
  "token": "eyJhbGciOi..."
}
```

### 4. Testar endpoint protegido

Substitua `<TOKEN>` pelo token retornado no login:

```bash
curl http://localhost:5001/users/me \
  -H "Authorization: Bearer <TOKEN>"
```

### 5. Listar jogos cadastrados no catálogo

```bash
curl http://localhost:5002/games
```

A API de catálogo possui seed inicial de jogos.

### 6. Cadastrar um novo jogo

```bash
curl -X POST http://localhost:5002/games \
  -H "Content-Type: application/json" \
  -d '{"title":"FIAP Quest","description":"Jogo acadêmico de aventura.","price":99.90,"stock":10}'
```

Esse endpoint também publica um evento no RabbitMQ.

### 7. Criar um pagamento

Use um `userId` e um `gameId` em formato GUID. O `gameId` pode ser copiado da resposta do catálogo.

```bash
curl -X POST http://localhost:5003/payments \
  -H "Content-Type: application/json" \
  -d '{"userId":"11111111-1111-1111-1111-111111111111","gameId":"22222222-2222-2222-2222-222222222222","amount":99.90}'
```

O pagamento é aprovado automaticamente quando o valor é maior que zero.

### 8. Listar pagamentos

```bash
curl http://localhost:5003/payments
```

### 9. Criar uma notificação

```bash
curl -X POST http://localhost:5004/notifications \
  -H "Content-Type: application/json" \
  -d '{"userId":"11111111-1111-1111-1111-111111111111","channel":"email","message":"Pagamento aprovado com sucesso."}'
```

### 10. Listar notificações

```bash
curl http://localhost:5004/notifications
```

### 11. Marcar notificação como lida

Substitua `<NOTIFICATION_ID>` pelo ID retornado na criação/listagem:

```bash
curl -X PUT http://localhost:5004/notifications/<NOTIFICATION_ID>/read
```

---

## Endpoints disponíveis

### Users API - `fcg-users-api`

Base URL via Docker Compose: `http://localhost:5001`

| Método | Endpoint | Descrição | Autenticação |
|---|---|---|---|
| GET | `/health` | Verifica saúde do serviço | Não |
| POST | `/users/register` | Cadastra usuário | Não |
| POST | `/users/login` | Realiza login e retorna JWT | Não |
| GET | `/users/me` | Retorna dados do usuário autenticado | Sim |

### Catalog API - `fcg-catalog-api`

Base URL via Docker Compose: `http://localhost:5002`

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/health` | Verifica saúde do serviço |
| GET | `/games` | Lista jogos |
| GET | `/games/{id}` | Busca jogo por ID |
| POST | `/games` | Cria jogo |
| PUT | `/games/{id}/stock` | Atualiza estoque |

### Payments API - `fcg-payments-api`

Base URL via Docker Compose: `http://localhost:5003`

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/health` | Verifica saúde do serviço |
| GET | `/payments` | Lista pagamentos |
| GET | `/payments/{id}` | Busca pagamento por ID |
| POST | `/payments` | Cria pagamento |

### Notifications API - `fcg-notifications-api`

Base URL via Docker Compose: `http://localhost:5004`

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/health` | Verifica saúde do serviço |
| GET | `/notifications` | Lista notificações |
| POST | `/notifications` | Cria notificação |
| PUT | `/notifications/{id}/read` | Marca notificação como lida |

---

## Exemplos de requisições

### Cadastro de usuário

```json
{
  "name": "Professor FIAP",
  "email": "professor@fiap.com.br",
  "password": "123456"
}
```

### Login

```json
{
  "email": "professor@fiap.com.br",
  "password": "123456"
}
```

### Criação de jogo

```json
{
  "title": "FIAP Quest",
  "description": "Jogo acadêmico de aventura.",
  "price": 99.90,
  "stock": 10
}
```

### Atualização de estoque

```json
{
  "stock": 25
}
```

### Criação de pagamento

```json
{
  "userId": "11111111-1111-1111-1111-111111111111",
  "gameId": "22222222-2222-2222-2222-222222222222",
  "amount": 99.90
}
```

### Criação de notificação

```json
{
  "userId": "11111111-1111-1111-1111-111111111111",
  "channel": "email",
  "message": "Pagamento aprovado com sucesso."
}
```

---

## RabbitMQ

O RabbitMQ é usado como broker de mensagens para demonstrar comunicação assíncrona entre serviços.

No Docker Compose, o RabbitMQ sobe automaticamente:

- AMQP: `localhost:5672`
- Painel administrativo: `http://localhost:15672`
- Usuário: `guest`
- Senha: `guest`

Serviços que publicam eventos:

- `fcg-catalog-api`: publica evento ao criar jogo e ao atualizar estoque.
- `fcg-payments-api`: publica evento ao criar pagamento.

A configuração usada no Docker Compose é:

```yaml
RabbitMq__Uri: "amqp://guest:guest@rabbitmq:5672"
```

---

## Banco de dados SQLite

Cada serviço possui seu próprio banco SQLite local, criado automaticamente ao iniciar a aplicação.

```text
fcg-users-api          -> banco de usuários
fcg-catalog-api        -> banco de catálogo
fcg-payments-api       -> banco de pagamentos
fcg-notifications-api  -> banco de notificações
```

Não é necessário executar scripts SQL manualmente. As tabelas são criadas automaticamente pelo Entity Framework Core durante a inicialização de cada API.

---

## Kubernetes

Os manifests Kubernetes estão na pasta:

```text
fcg-orchestration/k8s
```

Arquivos disponíveis:

```text
rabbitmq.yaml
users-api.yaml
apis.yaml
```

Para aplicar em um cluster Kubernetes local:

```bash
kubectl apply -f fcg-orchestration/k8s/rabbitmq.yaml
kubectl apply -f fcg-orchestration/k8s/users-api.yaml
kubectl apply -f fcg-orchestration/k8s/apis.yaml
```

Verificar pods e services:

```bash
kubectl get pods
kubectl get svc
```

Para remover:

```bash
kubectl delete -f fcg-orchestration/k8s/apis.yaml
kubectl delete -f fcg-orchestration/k8s/users-api.yaml
kubectl delete -f fcg-orchestration/k8s/rabbitmq.yaml
```

> Observação: os manifests usam imagens locais com `imagePullPolicy: IfNotPresent`. Em ambiente Minikube/Kind, pode ser necessário carregar as imagens no cluster ou configurar o Docker local do cluster antes de aplicar os manifests.

---

## Estrutura de pastas

```text
fiap-cloud-games/
├── FIAPCloudGames.sln
├── Dockerfile
├── docker-compose.yml
├── README.md
├── fcg-users-api/
│   ├── fcg-users-api.csproj
│   ├── Program.cs
│   └── appsettings.json
├── fcg-catalog-api/
│   ├── fcg-catalog-api.csproj
│   ├── Program.cs
│   └── appsettings.json
├── fcg-payments-api/
│   ├── fcg-payments-api.csproj
│   ├── Program.cs
│   └── appsettings.json
├── fcg-notifications-api/
│   ├── fcg-notifications-api.csproj
│   ├── Program.cs
│   └── appsettings.json
└── fcg-orchestration/
    └── k8s/
        ├── rabbitmq.yaml
        ├── users-api.yaml
        └── apis.yaml
```

---

## Comandos úteis

### Restaurar pacotes

```bash
dotnet restore
```

### Compilar solução

```bash
dotnet build FIAPCloudGames.sln
```

### Executar todos os serviços com Docker

```bash
docker compose up --build
```

### Executar em segundo plano

```bash
docker compose up --build -d
```

### Ver logs

```bash
docker compose logs -f
```

Logs de um serviço específico:

```bash
docker compose logs -f users-api
docker compose logs -f catalog-api
docker compose logs -f payments-api
docker compose logs -f notifications-api
```

### Parar containers

```bash
docker compose down
```

### Rebuild limpo

```bash
docker compose down --remove-orphans
docker compose build --no-cache
docker compose up
```

---

## Solução de problemas

### Porta já está em uso

Se alguma porta estiver ocupada, altere o mapeamento no `docker-compose.yml`.

Exemplo:

```yaml
ports:
  - "5011:8080"
```

Depois acesse `http://localhost:5011`.

### RabbitMQ ainda não iniciou totalmente

O RabbitMQ pode levar alguns segundos para ficar disponível. Se algum evento falhar logo na primeira tentativa, aguarde a inicialização completa e tente a requisição novamente.

### Swagger não abre

Confirme se os containers estão rodando:

```bash
docker compose ps
```

Verifique logs:

```bash
docker compose logs -f
```

### Build falhou no Docker

Tente limpar e reconstruir:

```bash
docker compose down --remove-orphans
docker compose build --no-cache
docker compose up
```

### Erro de autenticação no `/users/me`

Confirme se o header foi enviado corretamente:

```text
Authorization: Bearer <TOKEN>
```

O token deve ser exatamente o valor retornado pelo endpoint `/users/login`.

---

## Status do projeto

O projeto está funcional como MVP acadêmico, contemplando:

- Microserviços em .NET 8.
- APIs REST documentadas com Swagger.
- Persistência SQLite por serviço.
- Autenticação JWT no serviço de usuários.
- Mensageria com RabbitMQ.
- Docker Compose para execução completa.
- Manifests Kubernetes para orquestração.
