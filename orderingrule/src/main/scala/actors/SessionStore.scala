package actors


import akka.actor.typed.scaladsl.Behaviors
import akka.actor.typed.{ActorRef, Behavior}
import domain.{Player, Session}

import java.util.UUID

object SessionStore {

    sealed trait Message
    final case class AddSession(id: UUID, owner: Player, name: String, replayTo: ActorRef[Session]) extends Message
    final case class GetSession(id: UUID, replayTo: ActorRef[ActorRef[SessionActor.Command]]) extends Message
    final case class RemoveSession(id: UUID) extends Message



    def apply(sessions: Map[UUID, ActorRef[SessionActor.Command]] = Map.empty): Behavior[Message] = {
            Behaviors.receive { (context, message) =>
                message match {
                    case AddSession(id, owner, name, replayTo) =>
                        val session = Session.createDefault(id, owner, name)
                        val actor = context.spawn(SessionActor(session), id.toString)
                        replayTo ! session
                        apply(sessions.updated(id, actor))
                    case RemoveSession(id) => apply(sessions.removed(id))
                    case GetSession(id, replayTo) =>
                        replayTo ! sessions(id)
                        Behaviors.same
                }
            }
    }

}
