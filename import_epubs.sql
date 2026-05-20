-- Script SQL d'importation des EPUBs dans la base de données
-- Assurez-vous d'utiliser SQLite CLI pour exécuter ce script :
-- sqlite3 api.db < import_epubs.sql

-- Exemple d'insertion d'un EPUB. La fonction readfile() permet de lire le fichier et de l'insérer en tant que BLOB.
INSERT INTO Book (
    Title, 
    Author, 
    CoverImage,
    EpubFilePath, 
    TotalPages, 
    Description, 
    Language, 
    UploadedAt, 
    LastPageOpened, 
    LastOpenedDate, 
    InsertionDate, 
    EpubContent
) VALUES (
    'Livre Exemple', 
    'Auteur Inconnu', 
    'book_icon.png',
    'chemin/vers/livre.epub', 
    1, 
    'Un livre importé via SQL', 
    'fr', 
    datetime('now'), 
    0, 
    '0001-01-01 00:00:00', 
    datetime('now'), 
    readfile('chemin/vers/livre.epub')
);
