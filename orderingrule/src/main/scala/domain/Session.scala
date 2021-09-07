package domain

import java.util.UUID


case class Player(id: String, name: String, picture: String)

sealed trait SessionEvents

final case class PlayerAdded(player: Player) extends  SessionEvents
final case class GameSet(game: UUID) extends  SessionEvents
final case class StoryAdded(story: String) extends SessionEvents

case class Session(id: UUID, name: String, players: List[Player], stories: List[String], game: Option[UUID]) {

    def addPlayer(player: Player): Either[Validation, SessionEvents] = Right(PlayerAdded(player))
    def setGame(game: UUID): Either[Validation, SessionEvents] = Right(GameSet(game))
    def addStory(story: String): Either[Validation, SessionEvents] = Either.cond(story.nonEmpty, StoryAdded(story), InvalidStoryName)
}


object Session {
    def createDefault(name: String): Session = Session(UUID.randomUUID(), name, List.empty, List.empty, None)
}