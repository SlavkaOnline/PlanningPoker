package application

import domain.Session

import java.util.UUID

object Views  {
    final case class Player(id: String, name: String, picture: String)
    final case class Session(id: UUID, version: Long, owner: UUID, name: String, players: Array[Player], stories: Array[String], game: Option[UUID])

    def sessionViewMap(session: domain.Session): Session = Session(session.id, session.version, session.owner, session.name, session.players.map(p => Player(p.id.toString, p.name, p.picture)).toArray, session.stories.toArray, session.game )
}
