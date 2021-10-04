package application.dto

import domain.{ActiveGame, FinishedGame}

import java.util.UUID
import scala.collection.mutable.ArrayBuffer

object Views {
    final case class Player(id: UUID, name: String, picture: String)

    final case class Session(id: UUID, version: Long, owner: Player, name: String, players: Array[Player], stories: Array[String], gameId: Option[UUID])

    final case class Card(id: UUID, name: String)

    final case class Game(id: UUID, owner: UUID, name: String, columnCount: Int, cards: Array[Array[Card]], currentPlayer: Option[UUID], finishedPlayers: Array[UUID], isGameFinished: Boolean)

    def sessionToViewMap(session: domain.Session): Session = Session(session.id, session.events.length - 1, Player(session.owner.id, session.owner.name, session.owner.picture), session.name, session.players.map(p => Player(p.id, p.name, p.picture)).toArray, session.stories.toArray, session.gameId)

    def cardToViewMap(card: domain.Card): Card = Card(card.id, card.name)

    def gameToViewMap(game: domain.Game): Game = Game(
        id = game.id,
        owner = game.owner,
        name = game.name,
        columnCount = game.columnCount,
        cards = game.cards.values.foldLeft(Array(game.columnCount).map(_ => ArrayBuffer.empty[Card])) { (arr, card) =>
            arr(card.column) += cardToViewMap(card)
            arr
        }.map(_.toArray),
        currentPlayer = game.process match {
            case ActiveGame(currentPlayer, _, _) => Some(currentPlayer)
            case FinishedGame => None
        },
        finishedPlayers = game.process match {
            case ActiveGame(_, _, finishedPlayers) => finishedPlayers.toArray
            case _ => Array.empty
        },
        isGameFinished = game.process match {
            case _: ActiveGame => false
            case _ => true
        })
}
