package application.dto

import java.util.UUID

object Requests {
    final case class CreateSession(name: String)

    final case class AddStory(name: String)

    final case class RemoveStory(id: UUID)

    final case class StartGame(name: String, columnsCount: Int, cards: Array[String])

    final case class MoveCard(direction: String)

}
