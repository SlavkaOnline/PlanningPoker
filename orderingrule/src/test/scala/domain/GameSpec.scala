package domain

import org.scalatest.EitherValues
import org.scalatest.flatspec.AnyFlatSpec
import org.scalatest.matchers.should.Matchers

import java.util.UUID

class GameSpec extends AnyFlatSpec with Matchers with EitherValues {
    "The game" should "not be created with one player" in {
        val result = Game.create(UUID.randomUUID(), UUID.randomUUID(), Array(UUID.randomUUID()), 2, Array("Card1", "Card2"))
        result.left.value should be(NotEnoughPlayers)
    }

    it should "not be created with one ore zero card" in {
        val result = Game.create(UUID.randomUUID(), UUID.randomUUID(), Array(UUID.randomUUID(), UUID.randomUUID()), 2, Array("Card1"))
        result.left.value should be(NotEnoughCards)
    }

    it should "not be created with one ore zero column" in {
        val result = Game.create(UUID.randomUUID(), UUID.randomUUID(), Array(UUID.randomUUID(), UUID.randomUUID()), 1, Array("Card1", "Card2"))
        result.left.value should be(NotEnoughColumns)
    }

    it should "be created with valid params" in {
        val result = Game.create(UUID.randomUUID(), UUID.randomUUID(), Array(UUID.randomUUID(), UUID.randomUUID()), 2, Array("Card1", "Card2"))
        result.isRight should be (true)
    }

    "The next action" should "not be using not current player" in {
        for {
            game <- Game.create(UUID.randomUUID(), UUID.randomUUID(), Array(UUID.randomUUID(), UUID.randomUUID()), 2, Array("Card1", "Card2"))
            result <- game.next(UUID.randomUUID())
        } yield result.leftSideValue should be(NotYourTurn)
    }

    "The card" should "not be moved not current player" in {
        for {
            game <- Game.create(UUID.randomUUID(), UUID.randomUUID(), Array(UUID.randomUUID(), UUID.randomUUID()), 2, Array("Card1", "Card2"))
            result <- game.moveCard(UUID.randomUUID(), game.cards.head._1, MoveDirection.Right)
        } yield result.leftSideValue should be(NotYourTurn)
    }

    "The current player disconnected and the next" should "set current" in {
        val current = UUID.randomUUID()
        val next = UUID.randomUUID()
        for {
            game <- Game.create(UUID.randomUUID(), UUID.randomUUID(), Array(current, next, UUID.randomUUID()), 2, Array("Card1", "Card2"))
            game <- Right(game.disconnectPlayer(current))
            result = game.process match {
                case ActiveGame(currentPlayer, activePlayers, _) => currentPlayer == next && activePlayers.length == 1
                case FinishedGame => true
            }
        } yield result should be(true)
    }

    "The current player disconnected and last active" should "set current" in {
        val current = UUID.randomUUID()
        val next = UUID.randomUUID()
        for {
            game <- Game.create(UUID.randomUUID(), UUID.randomUUID(), Array(current, next), 2, Array("Card1", "Card2"))
            game <- Right(game.disconnectPlayer(current))
            result = game.process match {
                case ActiveGame(currentPlayer, activePlayers, _) => currentPlayer == next && activePlayers.isEmpty
                case FinishedGame => true
            }
        } yield result should be(true)
    }

    "The current player disconnected when active is empty" should "set the game is finished" in {
        val current = UUID.randomUUID()
        val next = UUID.randomUUID()
        for {
            game <- Game.create(UUID.randomUUID(), UUID.randomUUID(), Array(current, next), 2, Array("Card1", "Card2"))
            game <- game.next(current)
            game <- Right(game.disconnectPlayer(next))
            result = game.process match {
                case ActiveGame(_, _, _) => false
                case FinishedGame => true
            }
        } yield result should be(true)
    }

    "The card on the right border" should "not be moved" in {
        val current = UUID.randomUUID()
        for {
            game <- Game.create(UUID.randomUUID(), UUID.randomUUID(), Array(current, UUID.randomUUID()), 2, Array("Card1", "Card2"))
            game <- Right(game.copy(cards = game.cards.map(c => c._1 -> c._2.copy(column = 1))))
            result <- game.moveCard(current, game.cards.head._1, MoveDirection.Right)
        } yield result.leftSideValue should be(IncorrectCardMoving)
    }

    "The card on the left border" should "not be moved" in {
        val current = UUID.randomUUID()
        for {
            game <- Game.create(UUID.randomUUID(), UUID.randomUUID(), Array(current, UUID.randomUUID()), 2, Array("Card1", "Card2"))
            game <- Right(game.copy(cards = game.cards.map(c => c._1 -> c._2.copy(column = 0))))
            result <- game.moveCard(current, game.cards.head._1, MoveDirection.Left)
        } yield result.leftSideValue should be(IncorrectCardMoving)
    }

    "After card moving finished player" should "be moved to active" in {
        val players = Array(3).map(_ => UUID.randomUUID())
        for {
            game <- Game.create(UUID.randomUUID(), UUID.randomUUID(), players, 2, Array("Card1", "Card2"))
            game <- Right(game.copy(cards = game.cards.map(c => c._1 -> c._2.copy(column = 0))))
            game <- game.next(players(0))
            game <- game.next(players(1))
            game <- game.moveCard(players(2), game.cards.head._1, MoveDirection.Left)
            result = game.process match {
                case ActiveGame(_, activePlayers, finishedPlayers) => activePlayers.length == 2 && finishedPlayers.isEmpty
                case FinishedGame => false
            }
        } yield result should be(true)
    }
    "The current player after next action" should "be in finished" in {
        val current = UUID.randomUUID()
        val players = Array(current).concat(Array(3).map(_ => UUID.randomUUID()))
        for {
            game <- Game.create(UUID.randomUUID(), UUID.randomUUID(), players, 2, Array("Card1", "Card2"))
            game <- Right(game.copy(cards = game.cards.map(c => c._1 -> c._2.copy(column = 0))))
            game <- game.moveCard(current, game.cards.head._1, MoveDirection.Left)
            game <- game.next(current)
            result = game.process match {
                case ActiveGame(currentPlayer, activePlayers, finishedPlayers) => currentPlayer == players(1) && activePlayers.length == 2 && finishedPlayers.contains(current)
                case FinishedGame => false
            }
        } yield result should be(true)
    }
}


