package actors

import akka.actor.typed.scaladsl.Behaviors
import akka.actor.typed.{ActorRef, Behavior}
import akka.persistence.typed.scaladsl.EventSourcedBehavior
import akka.persistence.typed.scaladsl.Effect
import akka.persistence.typed.PersistenceId
import domain._

import java.util.UUID

object SessionActor {

    sealed trait Command
    final case class AddStory(playerId: UUID, story: String, replayTo: ActorRef[Session]) extends Command
    final case class GetSnapshot(replayTo: ActorRef[Session]) extends Command


    val commandHandler: (Session, Command) => Effect[SessionEvent, Session] = { (session, command) =>
        command match {
            case AddStory(playerId, story, replayTo) => session.addStory(playerId, story) match {
                case Right(event) => Effect.persist(event).thenReply(replayTo)(state => state)
                case Left(err) => Effect.unhandled
            }
            case GetSnapshot(replayTo) =>
                Effect.reply(replayTo)(session)
        }
    }


    def apply(session: Session): Behavior[Command] = {
        Behaviors.setup { context =>
            EventSourcedBehavior[Command, SessionEvent, Session](
                persistenceId = PersistenceId.ofUniqueId(session.id.toString),
                emptyState = session,
                eventHandler = Session.applyEvent,
                commandHandler = commandHandler
            )
        }
    }

}
