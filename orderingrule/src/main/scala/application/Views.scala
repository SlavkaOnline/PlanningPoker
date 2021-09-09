package application

import java.util.UUID

object Views  {
    final case class PlayerView(id: String, name: String, picture: String)
    final case class SessionView(id: UUID, version: Long, owner: UUID, name: String, players: Array[PlayerView], game: Option[UUID])

}
