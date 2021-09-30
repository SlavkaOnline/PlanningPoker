package domain

import java.util.UUID


case class Player(id: UUID, name: String, picture: String)

sealed trait SessionEvent

final case class PlayerJoined(player: Player) extends SessionEvent
final case class PlayerLeft(player: Player) extends SessionEvent
final case class GameSet(gameId: UUID) extends SessionEvent
final case class StoryAdded(story: String) extends SessionEvent

case class Session(id: UUID, owner: Player, name: String, players: List[Player], stories: List[String], gameId: Option[UUID], events: List[SessionEvent]) {

    private def validateOwner(owner: Player, playerId: UUID) = Either.cond(owner.id == playerId, playerId, UnauthorizedAccess)
    private def validateStoryName(story: String) = Either.cond(story.nonEmpty, story, InvalidStoryName)
    private def validateDuplicateStoryName(stories: List[String], story: String) = Either.cond(!stories.contains(story), story, StoryAlreadyExists)

    def joinPlayer(player: Player): Session = copy(players = player :: players, events = PlayerJoined(player) :: events)
    def leftPlayer(player: Player): Session = copy(players = players.filterNot(_ == player), events = PlayerLeft(player) :: events )

    def setGame(playerId: UUID, gameId: UUID): Either[Validation, Session] = validateOwner(owner, playerId).map(_ => copy(gameId = Some(gameId), events = GameSet(gameId) :: events))


    def addStory(playerId: UUID, story: String): Either[Validation, Session] = for {
        _ <- validateOwner(owner, playerId)
        story <- validateStoryName(story)
        story <- validateDuplicateStoryName(stories, story)
    } yield copy(stories = story :: stories, events =  StoryAdded(story) :: events)
}


object Session {
    def createDefault(id: UUID, owner: Player, name: String): Session = Session(id, owner, name, List.empty, List.empty, None, List.empty)
}