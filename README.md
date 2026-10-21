# Spotifei Família

Sistema em C# (.NET 9) que simula um streaming de música com controle parental, inspirado no plano família de serviços como o Spotify. Persiste os dados em um banco MySQL.

O backend principal é a **`SpotifeiFamilia.WebApi`** (API REST com autenticação JWT) — é nela que novas funcionalidades devem ser adicionadas primeiro. Existe também uma aplicação de console (`SpotifeiFamilia/`) sobre a mesma camada de negócio, mantida só como ferramenta de dev/demonstração (não recebe paridade de features com a WebApi).

## Funcionalidades

### Autenticação
- Cadastro e login por e-mail/senha.
- **Autenticação em dois fatores (2FA)** via TOTP (RFC 6238): no cadastro (ou no primeiro login de contas antigas), o sistema gera uma chave secreta e mostra um QR code local (aberto automaticamente no Chrome, com fallback pro navegador padrão e pra chave manual em texto). O código de 6 dígitos do app autenticador é exigido em todo login.
- **Limite de tentativas de login**: após 5 tentativas incorretas seguidas, a conta é bloqueada temporariamente por 2 horas (liberada automaticamente ao expirar o prazo).
- Campos de cadastro podem ser cancelados a qualquer momento digitando `cancelar`, sem precisar fechar o programa.

### Planos e assinatura
- Três planos disponíveis no cadastro: BASICO, PADRAO e PREMIUM (com preços cadastrados na tabela `plano`).

### Música
- Listar músicas disponíveis e buscar por título.
- Reproduzir uma música (registra no histórico de reprodução).
- Criar e gerenciar playlists (criar, listar, ver músicas de uma playlist, adicionar música a uma playlist).

### Spotifei Família (controle parental)
- Uma conta "responsável" pode adicionar/remover membros (contas filhas) vinculados a ela.
- Reautenticação por senha antes de entrar no painel de família.
- Bloquear/desbloquear artistas específicos ou conteúdo explícito por conta filha.
- Definir um limite diário de reproduções por conta filha (contabilizado por dia).
- Ver o histórico de reprodução de um membro da família.

## Stack

- **.NET 9** / C#
- **MySQL** — persistência via **Entity Framework Core** (`Pomelo.EntityFrameworkCore.MySql`); sem SQL cru
- **TwoFactorAuth.Net** — geração e verificação de códigos TOTP
- **QRCoder** — geração local de QR code (sem depender de serviços externos)

## Arquitetura

O código é dividido em projetos (`SpotifeiFamilia.sln`):

```
SpotifeiFamilia.Model/      # entidades puras (Usuario, Musica, Playlist, RestricaoConta, ...)
SpotifeiFamilia.Data/       # DAL — SpotifeiFamiliaContext (EF Core) + Repositories/
SpotifeiFamilia.Service/    # TotpService — geração/verificação do 2FA e QR code (compartilhado)
SpotifeiFamilia.Business/   # regras de negócio (Autenticacao, Familia, Restricao, Musica, Playlist)
SpotifeiFamilia.Tests/       # testes automatizados (xUnit) da camada de negócio
SpotifeiFamilia.Tools/       # utilitário de dev: gera o código 2FA de uma conta de teste local
SpotifeiFamilia/            # app de console
├── Views/                    # só entrada/saída no console
├── Controllers/               # orquestram o fluxo: leem input, validam, chamam a Business
└── Program.cs
SpotifeiFamilia.WebApi/      # API REST (Minimal APIs) com autenticação JWT
```

O schema (`bd.sql`) continua sendo a fonte da verdade do banco — o `SpotifeiFamiliaContext` só mapeia por cima das tabelas/colunas já existentes via Fluent API (`OnModelCreating`), sem migrations do EF.

| Camada | Projeto | Responsabilidade |
|---|---|---|
| **Model** | `SpotifeiFamilia.Model` | Entidades e enums, sem lógica |
| **Business** | `SpotifeiFamilia.Business` | Regras de negócio, validações e orquestração — usada tanto pela WebApi quanto pelo console |
| **API REST** | `SpotifeiFamilia.WebApi` | Backend principal: endpoints HTTP/JSON com autenticação JWT |
| **Repository (DAL)** | `SpotifeiFamilia.Data` | `UsuarioRepository`, `PlanoRepository`, `MusicaRepository`, `PlaylistRepository`, `FamiliaRepository`, `HistoricoRepository` |
| **View/Controller (dev)** | `SpotifeiFamilia` | Console mantido só como ferramenta de dev/demo: `AutenticacaoView`/`AutenticacaoController`, `MenuView`/`MenuController`, `MusicaView`/`MusicaController`, `PlaylistView`/`PlaylistController`, `FamiliaView`/`FamiliaController`, `RestricaoView`/`RestricaoController` |

Os testes de `SpotifeiFamilia.Tests` cobrem hoje a parte da Business que não depende de banco (`Validador`, `SenhaHasher`). As camadas `Repository` usam `new SpotifeiFamiliaContext()` direto, sem interface/DI — testar o restante da Business exigiria antes introduzir uma abstração de repositório (ou um banco de teste), o que é trabalho futuro em aberto.

## Como rodar

1. Tenha um MySQL local rodando. A connection string padrão (`SpotifeiFamilia.Data/SpotifeiFamiliaContext.cs`) assume `Server=127.0.0.1;Database=spotifeio;;Uid=root;Pwd=1234;`; para usar outra, defina a variável de ambiente `SPOTIFEIFAMILIA_DB_CONNECTION` (não é preciso editar o código).
2. Execute `bd.sql` — ele cria o schema completo e já popula com os dados de teste, tudo em um único script idempotente (pode rodar de novo sem duplicar nada).
3. Rode a WebApi (backend principal):

   ```
   dotnet run --project SpotifeiFamilia.WebApi
   ```

   Por padrão sobe com a chave JWT de desenvolvimento em `SpotifeiFamilia.WebApi/appsettings.json`. Em produção, sobrescreva-a com a variável de ambiente `Jwt__Key` (nunca use a chave de exemplo do repositório). Veja os endpoints disponíveis em `SpotifeiFamilia.WebApi/SpotifeiFamilia.WebApi.http` ou rode `testar_api.bat` para um smoke test automatizado (gera o código 2FA sozinho — veja abaixo).

   Login pela API exige o código de 6 dígitos do 2FA. Para testes locais sem precisar do app autenticador, `SpotifeiFamilia.Tools` calcula o código atual direto do secret salvo no banco (e gera um secret novo se a conta de teste ainda não tiver 2FA configurado):

   ```
   dotnet run --project SpotifeiFamilia.Tools -- pai@gmail.com
   ```

   Essa ferramenta só funciona porque já há acesso direto ao banco local — não é um jeito de burlar 2FA de contas de verdade, é puramente para automatizar o teste manual da API.

4. (Opcional) Rode o console, mantido só como ferramenta de dev/demo — não recebe novas features:

   ```
   dotnet run --project SpotifeiFamilia
   ```

   No cadastro, será necessário escanear o QR code exibido (ou usar a chave manual) com um app autenticador como Google Authenticator ou Authy para configurar o 2FA.

5. Para rodar os testes automatizados:

   ```
   dotnet test SpotifeiFamilia.Tests
   ```

### WebApi — autenticação e principais endpoints

- `POST /api/autenticacao/cadastro` — cria a conta e devolve o QR code/chave do 2FA (sem navegador, diferente do console); o 2FA é confirmado no primeiro login.
- `POST /api/autenticacao/login` — e-mail + senha + `codigo2fa` (quando a conta já tem 2FA configurado). Aplica o mesmo limite de 5 tentativas/bloqueio de 2h do console; contas sem 2FA ainda recebem um QR code na resposta e precisam logar de novo com o código.
- Os demais endpoints exigem `Authorization: Bearer <token>` e cobrem músicas, playlists (criar, listar, adicionar música) e o Spotifei Família completo: dependentes (listar/adicionar/remover), reautenticação, histórico, e restrições (bloquear/desbloquear artista ou conteúdo explícito, limite diário). Toda rota confere que o `usuarioId`/`responsavelId` da URL é o dono do token.
