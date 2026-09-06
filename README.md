# Spotifei Família

Aplicação de console em C# (.NET 9) que simula um sistema de streaming de música com controle parental, inspirado no plano família de serviços como o Spotify. Persiste os dados em um banco MySQL.

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

O código é dividido em três projetos (`SpotifeiFamilia.sln`):

```
SpotifeiFamilia.Model/     # entidades puras (Usuario, Musica, Playlist, RestricaoConta, ...)
SpotifeiFamilia.Data/      # DAL — SpotifeiFamiliaContext (EF Core) + Repositories/
SpotifeiFamilia/           # app de console
├── Services/TotpService.cs   # geração/verificação do 2FA e exibição do QR code
├── Views/                    # só entrada/saída no console
├── Controllers/               # orquestram o fluxo: leem input, validam, chamam os Repositories
└── Program.cs
```

O schema (`bd.sql`) continua sendo a fonte da verdade do banco — o `SpotifeiFamiliaContext` só mapeia por cima das tabelas/colunas já existentes via Fluent API (`OnModelCreating`), sem migrations do EF.

| Camada | Projeto | Responsabilidade |
|---|---|---|
| **Model** | `SpotifeiFamilia.Model` | Entidades e enums, sem lógica |
| **View** | `SpotifeiFamilia` | `AutenticacaoView`, `MenuView`, `MusicaView`, `PlaylistView`, `FamiliaView`, `RestricaoView` |
| **Controller** | `SpotifeiFamilia` | `AutenticacaoController`, `MenuController`, `MusicaController`, `PlaylistController`, `FamiliaController`, `RestricaoController` |
| **Repository (DAL)** | `SpotifeiFamilia.Data` | `UsuarioRepository`, `PlanoRepository`, `MusicaRepository`, `PlaylistRepository`, `FamiliaRepository`, `HistoricoRepository` |

## Como rodar

1. Tenha um MySQL local rodando (ajuste a connection string em `SpotifeiFamilia.Data/SpotifeiFamiliaContext.cs` se necessário).
2. Execute `bd.sql` para criar o schema e depois `popular_bd.sql` para os dados de teste.
3. Rode o projeto:

   ```
   dotnet run --project SpotifeiFamilia
   ```

4. No cadastro, será necessário escanear o QR code exibido (ou usar a chave manual) com um app autenticador como Google Authenticator ou Authy para configurar o 2FA.

### Já tinha o banco criado antes?

Se o schema `spotifeio` já existia no seu MySQL de uma versão anterior deste projeto (antes das colunas de 2FA, bloqueio por tentativas, Spotifei Família e conteúdo explícito existirem em `bd.sql`), rode `atualizar_schema.sql` uma única vez — ele só adiciona as colunas que faltam, sem apagar nada. Numa instalação nova (banco criado do zero com o `bd.sql` atual), esse script não é necessário.
