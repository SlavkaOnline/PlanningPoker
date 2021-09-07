package domain

import java.util.UUID


case class Player(id: String, name: String, picture: String)

sealed trait SessionEvents

final case class PlayerAdded(player: Player) extends  SessionEvents

case class Session(id: UUID, name: String, players: List[Player], stories: List[String], game: Option[Game]) {

    def AddPlayer(player: Player): Either[Validation, SessionEvents] = Right(PlayerAdded(player))
}
