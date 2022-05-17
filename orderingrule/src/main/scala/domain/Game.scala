package domain

import java.util.UUID
import scala.collection.immutable.HashMap
import scala.util.Random


object MoveDirection extends Enumeration {
    type MoveDirection = Value
    val Left, Right = Value
}

sealed trait GameProcess

case class ActiveGame(currentPlayer: UUID, activePlayers: List[UUID], finishedPlayers: List[UUID]) extends GameProcess {
    def clearFinished(): ActiveGame = copy(activePlayers = activePlayers.concat(finishedPlayers), finishedPlayers = List.empty)
}
case object FinishedGame extends GameProcess

case class Game private (id: UUID, owner: UUID, name:String, columnCount: Int, cards: HashMap[UUID,Card], process: GameProcess) {

    private def validateOwner(owner: UUID, player: UUID) = Either.cond(owner == player, player, NotYourTurn)
    private def validateCurrentPlayer(current: UUID, player: UUID) = Either.cond(current == player, player, NotYourTurn)
    private def validateCardId(cardId: UUID, cards: Set[UUID]) = Either.cond(cards.contains(cardId), cardId, CardIsNotExists)
    private def validateActiveGame(process: GameProcess) =
        process match {
            case p: ActiveGame => Right(p)
            case _ => Left(GameFinished)
        }

    def moveCard(playerId: UUID, cardId: UUID, direction: MoveDirection.MoveDirection): Either[Validation, Game] = {
        def tryCardMoving(direction: MoveDirection.MoveDirection, card: Card) = {
            direction match {
                case MoveDirection.Left => if (card.column - 1 >= 0) card.moveLeft() else Left(IncorrectCardMoving)
                case MoveDirection.Right => if (card.column + 1 < columnCount) card.moveRight() else Left(IncorrectCardMoving)
            }
        }

        for {
            p <- validateActiveGame(process)
            _ <- validateCurrentPlayer(p.currentPlayer, playerId)
            cardId <- validateCardId(cardId, cards.keySet)
            card <- tryCardMoving(direction, cards(cardId))
        } yield copy(cards = cards.updated(cardId, card), process = p.clearFinished())
    }

    def next(playerId: UUID): Either[Validation, Game] = {
        def moveCurrent(game: ActiveGame) = {
            if (game.activePlayers.isEmpty) {
                FinishedGame
            }
            else {
                ActiveGame(game.activePlayers.head, game.activePlayers.tail, game.currentPlayer :: game.finishedPlayers)
            }
        }

        for {
            p <- validateActiveGame(process)
            _ <- validateCurrentPlayer(p.currentPlayer, playerId).left.flatMap(_ => validateOwner(owner, playerId))
        } yield copy(process = moveCurrent(p), cards = cards.map(c => c._1 -> c._2.clearPosition()))
    }

    def leftPlayer(player: UUID): Game = {
        process match {
            case ActiveGame(current, active, finished) =>
                if (current == player) {
                    if (active.isEmpty) {
                        copy(process = FinishedGame)
                    }
                    else {
                        copy(process = ActiveGame(active.head, active.tail, finished))
                    }
                } else if (active.contains(player)) {
                    copy(process = ActiveGame(current, active.filter( _ != player), finished))
                } else if (finished.contains(player)) {
                    copy(process = ActiveGame(current, active, finished.filter( _ != player)))
                } else {
                    this
                }
            case _ => this
        }
    }

    def joinPlayer(player: UUID): Game = {
        process match {
            case ActiveGame(current, active, finished) =>
                    copy(process = ActiveGame(current, player :: active, finished))

            case _ => this
        }
    }

}

object Game {

    private def validatePlayers(players: Array[UUID]) = Either.cond(players.length >= 2, players, NotEnoughPlayers )
    private def validateColumnCount(columnCount: Int) = Either.cond(columnCount >= 2, columnCount, NotEnoughColumns)
    private def validateCards(cards: Array[String]) = Either.cond(cards.length>= 2, cards, NotEnoughCards)
    private def validateName(name: String) = Either.cond(name.nonEmpty, name, InvalidGameName)

    def create(id: UUID, owner: UUID, name: String, players: Array[UUID], columnCount: Int, nameCards: Array[String]): Either[Validation, Game] = {
        val rand = new Random()
        for {
            name <- validateName(name)
            players <- validatePlayers(players)
            columns <- validateColumnCount(columnCount)
            cards <- validateCards(nameCards).map(names => names.map(name => Card(UUID.randomUUID(), name, rand.nextInt(columnCount), Position.Current)))
        }
        yield  Game(id, owner, name, columns, HashMap(cards.map(c => c.id -> c): _*), ActiveGame(players.head,  players.tail.toList, List.empty))
    }
}

