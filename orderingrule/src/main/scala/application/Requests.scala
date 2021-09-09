package application

import java.util.UUID

object Requests  {
    final case class CreateSession(name: String)
    final case class AddStory(name: String)
    final case class RemoveStory(id: UUID)
}
