package domain

import java.util.UUID


case class Player(id: String, name: String, picture: String)

sealed trait SessionEvent

final case class PlayerJoined(player: Player) extends SessionEvent
final case class PlayerLeft(player: Player) extends SessionEvent
final case class GameSet(game: UUID) extends SessionEvent
final case class StoryAdded(story: String) extends SessionEvent

case class Session(id: UUID, owner: UUID, name: String, version: Long, players: List[Player], stories: List[String], game: Option[UUID]) {

    private def validationOwner(owner: UUID, playerId: UUID) = Either.cond(owner == playerId, playerId, UnauthorizedAccess)

    private def validateStoryName(story: String) = Either.cond(story.nonEmpty, story, InvalidStoryName)

    def joinPlayer(player: Player): Either[Validation, SessionEvent] = Right(PlayerJoined(player))

    def leftPlayer(player: Player): Either[Validation, SessionEvent] = Right(PlayerLeft(player))

    def setGame(game: UUID): Either[Validation, SessionEvent] = Right(GameSet(game))

    def addStory(playerId: UUID, story: String): Either[Validation, SessionEvent] = for {
        _ <- validationOwner(owner, playerId)
        story <- validateStoryName(story)
    } yield StoryAdded(story)
}


object Session {
    def createDefault(id: UUID, owner: UUID, name: String): Session = Session(id, owner, name, 0, List.empty, List.empty, None)

    def applyEvent(session: Session, event: SessionEvent): Session = {
        event match {
            case PlayerJoined(player) => session.copy(players = player :: session.players, version = session.version + 1)
            case PlayerLeft(player) => session.copy(players = session.players.filterNot(_ == player), version = session.version + 1)
            case GameSet(game) => session.copy(game = Some(game), version = session.version + 1)
            case StoryAdded(story) => session.copy(stories = story :: session.stories, version = session.version + 1)
        }
    }
}