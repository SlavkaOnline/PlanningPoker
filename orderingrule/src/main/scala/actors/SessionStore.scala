package actors

import akka.actor.typed.scaladsl.AskPattern.Askable
import akka.actor.typed.scaladsl.Behaviors
import akka.actor.typed.{ActorRef, Behavior}
import akka.util.Timeout
import application.Views.{PlayerView, SessionView}
import domain.Session

import java.util.UUID
import scala.concurrent.duration.DurationInt

object SessionStore {

    sealed trait Command
    final case class AddSession(id: UUID, owner: UUID, name: String, replayTo: ActorRef[SessionView]) extends Command
    final case class RemoveSession(id: UUID) extends Command



    def apply(): Behavior[Command] = sessionStore(Map.empty)

    def sessionStore(sessions: Map[UUID, ActorRef[SessionActor.Command]]): Behavior[Command] = {
            Behaviors.receive { (context, message) =>
                message match {
                    case AddSession(id, owner, name, replayTo) =>
                        val session = Session.createDefault(id, owner, name)
                        val actor = context.spawn(SessionActor(session), id.toString)
                        replayTo ! SessionView(session.id, session.version, session.owner, session.name, session.players.map(p => PlayerView(p.id, p.name, p.picture)).toArray, session.game )
                        sessionStore(sessions.updated(id, actor))
                    case RemoveSession(id) => sessionStore(sessions.removed(id))
                }
            }
    }

}
