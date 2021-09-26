package application

import domain.Session

import java.util.UUID

object Views  {
    final case class PlayerView(id: String, name: String, picture: String)
    final case class SessionView(id: UUID, version: Long, owner: UUID, name: String, players: Array[PlayerView], stories: Array[String], game: Option[UUID])

    def sessionViewMap(session: Session): SessionView = SessionView(session.id, session.version, session.owner, session.name, session.players.map(p => PlayerView(p.id, p.name, p.picture)).toArray, session.stories.toArray, session.game )
}
