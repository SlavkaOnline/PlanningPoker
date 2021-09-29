package application

import domain.Session

import java.util.UUID

object Views {
    final case class Player(id: UUID, name: String, picture: String)

    final case class Session(id: UUID, version: Long, owner: Player, name: String, players: Array[Player], stories: Array[String], gameId: Option[UUID])

    def sessionViewMap(session: domain.Session): Session = Session(session.id, session.version, Player(session.owner.id, session.owner.name, session.owner.picture), session.name, session.players.map(p => Player(p.id, p.name, p.picture)).toArray, session.stories.toArray, session.gameId)
}
