-- Script único do banco `spotifeio`: cria o schema completo (com todas as
-- colunas de 2FA, bloqueio por tentativas, Spotifei Família e conteúdo
-- explícito já incluídas) e popula com dados de teste. Idempotente: pode
-- rodar várias vezes sem duplicar linhas nem apagar dados existentes.
--
-- Uso: rode este arquivo inteiro no MySQL e pronto -- não é preciso nenhum
-- outro script.

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';

-- -----------------------------------------------------
-- Schema spotifeio
-- -----------------------------------------------------
CREATE SCHEMA IF NOT EXISTS `spotifeio` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci ;
USE `spotifeio` ;

-- -----------------------------------------------------
-- Table `spotifeio`.`albums`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `spotifeio`.`albums` (
  `id_album` INT NOT NULL AUTO_INCREMENT,
  `titulo` VARCHAR(100) NOT NULL,
  PRIMARY KEY (`id_album`))
ENGINE = InnoDB
AUTO_INCREMENT = 6
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `spotifeio`.`artists`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `spotifeio`.`artists` (
  `id_artist` INT NOT NULL AUTO_INCREMENT,
  `nome_artista` VARCHAR(50) NOT NULL,
  `estilo` VARCHAR(50) NOT NULL,
  `bio` TEXT NULL DEFAULT NULL,
  `imagem` VARCHAR(500) NULL DEFAULT NULL,
  PRIMARY KEY (`id_artist`))
ENGINE = InnoDB
AUTO_INCREMENT = 6
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `spotifeio`.`genres`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `spotifeio`.`genres` (
  `id_genre` INT NOT NULL AUTO_INCREMENT,
  `nome_genero` VARCHAR(50) NOT NULL,
  PRIMARY KEY (`id_genre`),
  UNIQUE INDEX `nome_genero` (`nome_genero` ASC) VISIBLE)
ENGINE = InnoDB
AUTO_INCREMENT = 6
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `spotifeio`.`plano`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `spotifeio`.`plano` (
  `id_plano` INT NOT NULL AUTO_INCREMENT,
  `nome_plano` ENUM('BASICO', 'PADRAO', 'PREMIUM') NOT NULL,
  `preco_mensal` DECIMAL(12,2) NULL DEFAULT NULL,
  PRIMARY KEY (`id_plano`),
  UNIQUE INDEX `nome_plano` (`nome_plano` ASC) VISIBLE)
ENGINE = InnoDB
AUTO_INCREMENT = 4
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;

INSERT INTO `spotifeio`.`plano` (`nome_plano`, `preco_mensal`) VALUES
  ('BASICO', 19.90),
  ('PADRAO', 29.90),
  ('PREMIUM', 39.90)
ON DUPLICATE KEY UPDATE `nome_plano` = `nome_plano`;


-- -----------------------------------------------------
-- Table `spotifeio`.`users`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `spotifeio`.`users` (
  `id_user` INT NOT NULL AUTO_INCREMENT,
  `nome_usuario` VARCHAR(50) NULL DEFAULT NULL,
  `CPF` VARCHAR(11) NULL DEFAULT NULL,
  `e_mail` VARCHAR(100) NULL DEFAULT NULL,
  `senha` VARCHAR(8) NULL DEFAULT NULL,
  `totp_secret` VARCHAR(64) NULL DEFAULT NULL COMMENT 'Chave secreta TOTP (Base32) usada para gerar/validar o código do segundo fator de autenticação (2FA).',
  `tentativas_login` INT NOT NULL DEFAULT 0 COMMENT 'Contador de tentativas de login incorretas consecutivas. Zerado a cada login bem-sucedido.',
  `bloqueado` TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Bloqueio manual/permanente feito pelo administrador diretamente no banco.',
  `bloqueado_ate` DATETIME NULL DEFAULT NULL COMMENT 'Bloqueio temporário por excesso de tentativas de login incorretas: login liberado automaticamente após esse horário.',
  `plano_id` INT NULL DEFAULT NULL,
  `responsavel_id` INT NULL DEFAULT NULL,
  `limite_diario_reproducoes` INT NULL DEFAULT NULL COMMENT 'Limite diário de músicas que a conta pode reproduzir. NULL = sem limite. Definido pela conta responsável (Spotifei Família).',
  PRIMARY KEY (`id_user`),
  UNIQUE INDEX `CPF` (`CPF` ASC) VISIBLE,
  UNIQUE INDEX `e_mail` (`e_mail` ASC) VISIBLE,
  INDEX `plano_id` (`plano_id` ASC) VISIBLE,
  INDEX `fk_users_responsavel` (`responsavel_id` ASC) VISIBLE,
  CONSTRAINT `fk_users_responsavel`
    FOREIGN KEY (`responsavel_id`)
    REFERENCES `spotifeio`.`users` (`id_user`),
  CONSTRAINT `users_ibfk_1`
    FOREIGN KEY (`plano_id`)
    REFERENCES `spotifeio`.`plano` (`id_plano`))
ENGINE = InnoDB
AUTO_INCREMENT = 8
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `spotifeio`.`playlists`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `spotifeio`.`playlists` (
  `id_playlist` INT NOT NULL AUTO_INCREMENT,
  `user_id` INT NOT NULL,
  `nome_playlist` VARCHAR(100) NOT NULL,
  PRIMARY KEY (`id_playlist`),
  INDEX `user_id` (`user_id` ASC) VISIBLE,
  CONSTRAINT `playlists_ibfk_1`
    FOREIGN KEY (`user_id`)
    REFERENCES `spotifeio`.`users` (`id_user`))
ENGINE = InnoDB
AUTO_INCREMENT = 4
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `spotifeio`.`tracks`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `spotifeio`.`tracks` (
  `id_track` INT NOT NULL AUTO_INCREMENT,
  `titulo` VARCHAR(100) NOT NULL,
  `album_id` INT NULL DEFAULT NULL,
  `explicito` TINYINT(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`id_track`),
  INDEX `album_id` (`album_id` ASC) VISIBLE,
  CONSTRAINT `tracks_ibfk_1`
    FOREIGN KEY (`album_id`)
    REFERENCES `spotifeio`.`albums` (`id_album`))
ENGINE = InnoDB
AUTO_INCREMENT = 26
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `spotifeio`.`playlist_tracks`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `spotifeio`.`playlist_tracks` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `playlist_id` INT NOT NULL,
  `track_id` INT NOT NULL,
  PRIMARY KEY (`id`),
  INDEX `playlist_id` (`playlist_id` ASC) VISIBLE,
  INDEX `track_id` (`track_id` ASC) VISIBLE,
  CONSTRAINT `playlist_tracks_ibfk_1`
    FOREIGN KEY (`playlist_id`)
    REFERENCES `spotifeio`.`playlists` (`id_playlist`),
  CONSTRAINT `playlist_tracks_ibfk_2`
    FOREIGN KEY (`track_id`)
    REFERENCES `spotifeio`.`tracks` (`id_track`))
ENGINE = InnoDB
AUTO_INCREMENT = 15
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `spotifeio`.`restricoes_conta`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `spotifeio`.`restricoes_conta` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `conta_filha_id` INT NOT NULL,
  `tipo` ENUM('ARTISTA', 'EXPLICITO_GERAL') NOT NULL,
  `artist_id` INT NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  INDEX `conta_filha_id` (`conta_filha_id` ASC) VISIBLE,
  INDEX `artist_id` (`artist_id` ASC) VISIBLE,
  CONSTRAINT `fk_restricoes_artist`
    FOREIGN KEY (`artist_id`)
    REFERENCES `spotifeio`.`artists` (`id_artist`),
  CONSTRAINT `fk_restricoes_conta_filha`
    FOREIGN KEY (`conta_filha_id`)
    REFERENCES `spotifeio`.`users` (`id_user`)
    ON DELETE CASCADE)
ENGINE = InnoDB
AUTO_INCREMENT = 2
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `spotifeio`.`track_artists`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `spotifeio`.`track_artists` (
  `track_id` INT NOT NULL,
  `artist_id` INT NOT NULL,
  `papel` ENUM('PRINCIPAL', 'FEAT') NULL DEFAULT 'PRINCIPAL',
  PRIMARY KEY (`track_id`, `artist_id`),
  INDEX `artist_id` (`artist_id` ASC) VISIBLE,
  CONSTRAINT `track_artists_ibfk_1`
    FOREIGN KEY (`track_id`)
    REFERENCES `spotifeio`.`tracks` (`id_track`),
  CONSTRAINT `track_artists_ibfk_2`
    FOREIGN KEY (`artist_id`)
    REFERENCES `spotifeio`.`artists` (`id_artist`))
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `spotifeio`.`historico_reproducao`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `spotifeio`.`historico_reproducao` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `user_id` INT NOT NULL,
  `track_id` INT NOT NULL,
  `data_hora` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  INDEX `user_id` (`user_id` ASC) VISIBLE,
  INDEX `track_id` (`track_id` ASC) VISIBLE,
  CONSTRAINT `fk_historico_user`
    FOREIGN KEY (`user_id`)
    REFERENCES `spotifeio`.`users` (`id_user`)
    ON DELETE CASCADE,
  CONSTRAINT `fk_historico_track`
    FOREIGN KEY (`track_id`)
    REFERENCES `spotifeio`.`tracks` (`id_track`))
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `spotifeio`.`contagem_diaria_reproducoes`
-- -----------------------------------------------------
-- Guarda quantas músicas cada usuário reproduziu em cada dia.
-- É incrementada a cada reprodução (ver ReproduzirMusica) e usada para
-- aplicar o limite diário definido em `users`.`limite_diario_reproducoes`.
-- Fica registrada no banco para consulta/manipulação pelo desenvolvedor.
CREATE TABLE IF NOT EXISTS `spotifeio`.`contagem_diaria_reproducoes` (
  `user_id` INT NOT NULL,
  `data` DATE NOT NULL,
  `quantidade` INT NOT NULL DEFAULT 0,
  PRIMARY KEY (`user_id`, `data`),
  CONSTRAINT `fk_contagem_diaria_user`
    FOREIGN KEY (`user_id`)
    REFERENCES `spotifeio`.`users` (`id_user`)
    ON DELETE CASCADE)
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;


-- =======================================================
-- Dados de teste
-- =======================================================
USE `spotifeio`;

-- -----------------------------------------------------
-- Gêneros
-- -----------------------------------------------------
INSERT INTO `genres` (`id_genre`, `nome_genero`) VALUES
  (1, 'Pop'),
  (2, 'Sertanejo'),
  (3, 'Rap'),
  (4, 'Rock'),
  (5, 'Eletrônica')
AS novo
ON DUPLICATE KEY UPDATE `nome_genero` = novo.`nome_genero`;

-- -----------------------------------------------------
-- Artistas
-- -----------------------------------------------------
INSERT INTO `artists` (`id_artist`, `nome_artista`, `estilo`, `bio`, `imagem`) VALUES
  (1, 'Ana Vitória', 'Pop', NULL, NULL),
  (2, 'Turma do Pagode', 'Sertanejo', NULL, NULL),
  (3, 'MC Explícito', 'Rap', NULL, NULL),
  (4, 'Banda Rock Local', 'Rock', NULL, NULL),
  (5, 'DJ Voltz', 'Eletrônica', NULL, NULL)
AS novo
ON DUPLICATE KEY UPDATE `nome_artista` = novo.`nome_artista`;

-- -----------------------------------------------------
-- Álbuns
-- -----------------------------------------------------
INSERT INTO `albums` (`id_album`, `titulo`) VALUES
  (1, 'Ao Vivo em SP'),
  (2, 'Raiz Sertaneja'),
  (3, 'Trap Pesado'),
  (4, 'Rock de Garagem'),
  (5, 'Batidas Eletrônicas')
AS novo
ON DUPLICATE KEY UPDATE `titulo` = novo.`titulo`;

-- -----------------------------------------------------
-- Faixas (5 e 6 são explícitas, de propósito, pra testar restrição)
-- -----------------------------------------------------
INSERT INTO `tracks` (`id_track`, `titulo`, `album_id`, `explicito`) VALUES
  (1, 'Coração em Chamas', 1, 0),
  (2, 'Não Vou Voltar', 1, 0),
  (3, 'Modão de Raiz', 2, 0),
  (4, 'Sofrência Total', 2, 0),
  (5, 'Trap da Quebrada', 3, 1),
  (6, 'Flow Pesado', 3, 1),
  (7, 'Riff Perdido', 4, 0),
  (8, 'Garagem Cheia', 4, 0),
  (9, 'Batida 128', 5, 0),
  (10, 'Drop Insano', 5, 0)
AS novo
ON DUPLICATE KEY UPDATE `titulo` = novo.`titulo`;

-- -----------------------------------------------------
-- Relação faixa <-> artista
-- Track 6 tem MC Explícito (principal) + Ana Vitória (feat), útil pra testar
-- que bloquear um artista esconde a faixa mesmo em feat.
-- -----------------------------------------------------
INSERT INTO `track_artists` (`track_id`, `artist_id`, `papel`) VALUES
  (1, 1, 'PRINCIPAL'),
  (2, 1, 'PRINCIPAL'),
  (3, 2, 'PRINCIPAL'),
  (4, 2, 'PRINCIPAL'),
  (5, 3, 'PRINCIPAL'),
  (6, 3, 'PRINCIPAL'),
  (6, 1, 'FEAT'),
  (7, 4, 'PRINCIPAL'),
  (8, 4, 'PRINCIPAL'),
  (9, 5, 'PRINCIPAL'),
  (10, 5, 'PRINCIPAL')
AS novo
ON DUPLICATE KEY UPDATE `papel` = novo.`papel`;
