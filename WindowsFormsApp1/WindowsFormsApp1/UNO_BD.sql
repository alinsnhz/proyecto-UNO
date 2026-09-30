CREATE TABLE Jugador (
    IdJugador INT AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE Partida (
    IdPartida INT AUTO_INCREMENT PRIMARY KEY,
    Fecha DATETIME NOT NULL,
    IdJugadorGanador INT,
    FOREIGN KEY (IdJugadorGanador) REFERENCES Jugador(IdJugador)
);

CREATE TABLE ResultadoPartida (
    IdResultado INT AUTO_INCREMENT PRIMARY KEY,
    IdPartida INT NOT NULL,
    IdJugador INT NOT NULL,
    CartasRestantes INT NOT NULL,
    FOREIGN KEY (IdPartida) REFERENCES Partida(IdPartida),
    FOREIGN KEY (IdJugador) REFERENCES Jugador(IdJugador)
);

CREATE TABLE LogJuego (
    IdLog INT AUTO_INCREMENT PRIMARY KEY,
    IdPartida INT,
    Fecha DATETIME NOT NULL,
    Mensaje VARCHAR(255) NOT NULL,
    FOREIGN KEY (IdPartida) REFERENCES Partida(IdPartida)
);

INSERT INTO Jugador (Nombre) VALUES ('Jugador1'), ('Jugador2'), ('Jugador3');
 
SELECT * FROM Jugador;

ALTER TABLE ResultadoPartida
ADD COLUMN Gano BOOLEAN NOT NULL DEFAULT FALSE;

DESCRIBE resultadopartida;