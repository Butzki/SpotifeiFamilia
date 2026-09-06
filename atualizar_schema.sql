-- O banco `spotifeio` já existente foi criado com uma versão mais antiga do
-- schema (antes das colunas de 2FA, bloqueio por tentativas, Spotifei Família
-- e conteúdo explícito existirem em bd.sql). Este script só ADICIONA as
-- colunas que faltam, sem apagar nada -- seguro rodar mesmo com dados já
-- cadastrados (users, tracks, etc.).
--
-- Já foi aplicado neste ambiente (02/09/2026). Rodar de novo numa base onde as
-- colunas já existem vai dar erro "Duplicate column name" -- isso é esperado
-- e inofensivo, só confirma que não há nada a fazer. Numa base criada do zero
-- só com bd.sql, este script não é necessário.

USE `spotifeio`;

ALTER TABLE `users`
    ADD COLUMN `totp_secret` VARCHAR(64) NULL DEFAULT NULL
        COMMENT 'Chave secreta TOTP (Base32) usada para gerar/validar o código do segundo fator de autenticação (2FA).',
    ADD COLUMN `tentativas_login` INT NOT NULL DEFAULT 0
        COMMENT 'Contador de tentativas de login incorretas consecutivas. Zerado a cada login bem-sucedido.',
    ADD COLUMN `bloqueado` TINYINT(1) NOT NULL DEFAULT 0
        COMMENT 'Bloqueio manual/permanente feito pelo administrador diretamente no banco.',
    ADD COLUMN `bloqueado_ate` DATETIME NULL DEFAULT NULL
        COMMENT 'Bloqueio temporário por excesso de tentativas de login incorretas: login liberado automaticamente após esse horário.',
    ADD COLUMN `responsavel_id` INT NULL DEFAULT NULL,
    ADD COLUMN `limite_diario_reproducoes` INT NULL DEFAULT NULL
        COMMENT 'Limite diário de músicas que a conta pode reproduzir. NULL = sem limite. Definido pela conta responsável (Spotifei Família).';

-- Adiciona a FK de auto-relacionamento (conta filha -> conta responsável) só se ainda não existir.
SET @fk_existe = (
    SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = 'spotifeio' AND TABLE_NAME = 'users' AND CONSTRAINT_NAME = 'fk_users_responsavel'
);
SET @sql = IF(@fk_existe = 0,
    'ALTER TABLE `users` ADD CONSTRAINT `fk_users_responsavel` FOREIGN KEY (`responsavel_id`) REFERENCES `users` (`id_user`)',
    'SELECT ''fk_users_responsavel ja existe'''
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

ALTER TABLE `tracks`
    ADD COLUMN `explicito` TINYINT(1) NOT NULL DEFAULT '0';
