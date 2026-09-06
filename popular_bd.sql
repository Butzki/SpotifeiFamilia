-- Script de dados de teste para o schema `spotifeio` (bd.sql).
-- Idempotente: pode rodar várias vezes sem duplicar linhas.
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
