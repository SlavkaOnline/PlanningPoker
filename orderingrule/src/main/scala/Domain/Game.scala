package Domain

import java.util.UUID
import scala.collection.immutable.HashMap
import scala.util.Random

case class Game(owner: UUID, columnCount: Int, cards: HashMap[UUID,Card], activePlayers: List[UUID], currentPlayer: UUID, finishedPlayers: List[UUID]) {
    def next(player: UUID): Either[String, Game] = {

        def action() = {
            val current = activePlayers.head
            val active = currentPlayer :: activePlayers
            Right(copy(activePlayers = active, currentPlayer = current))
        }

        if (player != currentPlayer && player != owner) {
            return Left("You cannot take an action outside of your turn.")
        } else if (player == owner) {
            return action()
        } else if (cards.values.forall(c => c.position == Position.Current))
        {
          return Left("You need to move at least one card or skip a turn")
        }
        action()

    }

    def skip()
}

object Game {

    private def validatePlayers(players: Array[UUID]) =  if (players.length < 2 ) Left  ("Not enough players to start the game, min 2") else Right(players)
    private def validateColumnCount(columnCount: Int) = if (columnCount < 2 ) Left  ("Not enough columns to start the game, min 2") else Right(columnCount)
    private def validateCards(cards: Array[String]) = if (cards.length < 2 ) Left  ("Not enough cards to start the game, min 2") else Right(cards)

    def create(owner: UUID,  players: Array[UUID], columnCount: Int, nameCards: Array[String]): Either[String, Game] = {
        val rand = new Random(columnCount)
        for {
            players <- validatePlayers(players)
            columns <- validateColumnCount(columnCount)
            cards <- validateCards(nameCards).map(names => names.map(name => Card(UUID.randomUUID(), name, rand.nextInt(), Position.Current)))
        }
        yield  Game (owner, columns, HashMap(cards.map(c => c.id -> c): _*), players.tail.toList, players.head, List.empty)
    }
}

