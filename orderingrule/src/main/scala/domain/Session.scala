package domain

import java.util.UUID


case class Player(id: UUID, name: String, picture: String)

sealed trait SessionEvent

final case class PlayerJoined(player: Player) extends SessionEvent
final case class PlayerLeft(player: Player) extends SessionEvent
final case class GameSet(gameId: UUID) extends SessionEvent
final case class StoryAdded(story: String) extends SessionEvent

case class Session(id: UUID, owner: Player, name: String, version: Long, players: List[Player], stories: List[String], gameId: Option[UUID]) {

    private def validateOwner(owner: Player, playerId: UUID) = Either.cond(owner.id == playerId, playerId, UnauthorizedAccess)
    private def validateStoryName(story: String) = Either.cond(story.nonEmpty, story, InvalidStoryName)
    private def validateDuplicateStoryName(stories: List[String], story: String) = Either.cond(!stories.contains(story), story, StoryAlreadyExists)

    def joinPlayer(player: Player): SessionEvent = PlayerJoined(player)
    def leftPlayer(player: Player): SessionEvent = PlayerLeft(player)

    def setGame(playerId: UUID, gameId: UUID): Either[Validation, SessionEvent] = validateOwner(owner, playerId).map(_ => GameSet(gameId))


    def addStory(playerId: UUID, story: String): Either[Validation, SessionEvent] = for {
        _ <- validateOwner(owner, playerId)
        story <- validateStoryName(story)
        story <- validateDuplicateStoryName(stories, story)
    } yield StoryAdded(story)
}


object Session {
    def createDefault(id: UUID, owner: Player, name: String): Session = Session(id, owner, name, 0, List.empty, List.empty, None)

    def applyEvent(session: Session, event: SessionEvent): Session = {
        event match {
            case PlayerJoined(player) => session.copy(players = player :: session.players, version = session.version + 1)
            case PlayerLeft(player) => session.copy(players = session.players.filterNot(_ == player), version = session.version + 1)
            case GameSet(gameId) => session.copy(gameId = Some(gameId), version = session.version + 1)
            case StoryAdded(story) => session.copy(stories = story :: session.stories, version = session.version + 1)
        }
    }
}