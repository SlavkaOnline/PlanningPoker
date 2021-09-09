package actors


import akka.actor.typed.scaladsl.Behaviors
import akka.actor.typed.{ActorRef, Behavior}
import application.Views.{PlayerView, SessionView}
import domain.Session

import java.util.UUID

object SessionStore {

    sealed trait Message
    final case class AddSession(id: UUID, owner: UUID, name: String, replayTo: ActorRef[SessionView]) extends Message
    final case class GetSession(id: UUID, replayTo: ActorRef[ActorRef[SessionActor.Command]]) extends Message
    final case class RemoveSession(id: UUID) extends Message


    def sessionViewMap(session: Session): SessionView = SessionView(session.id, session.version, session.owner, session.name, session.players.map(p => PlayerView(p.id, p.name, p.picture)).toArray, session.game )
    def apply(): Behavior[Message] = sessionStore(Map.empty)

    def sessionStore(sessions: Map[UUID, ActorRef[SessionActor.Command]]): Behavior[Message] = {
            Behaviors.receive { (context, message) =>
                message match {
                    case AddSession(id, owner, name, replayTo) =>
                        val session = Session.createDefault(id, owner, name)
                        val actor = context.spawn(SessionActor(session), id.toString)
                        replayTo ! sessionViewMap(session)
                        sessionStore(sessions.updated(id, actor))
                    case RemoveSession(id) => sessionStore(sessions.removed(id))
                    case GetSession(id, replayTo) =>
                        replayTo ! sessions(id)
                        Behaviors.same
                }
            }
    }

}
