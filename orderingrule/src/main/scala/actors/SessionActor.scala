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
    final case class AddStory(playerId: UUID, story: String, replayTo: ActorRef[Either[Validation, Session]]) extends Command
    final case class StartGame(playerId: UUID, gameId: UUID, replayTo: ActorRef[Either[Validation, Session]]) extends Command
    final case class JoinPlayer(player: Player) extends Command
    final case class LeftPlayer(player: Player) extends Command
    final case class GetSnapshot(replayTo: ActorRef[Session]) extends Command

    private def handleEvent(commandHandler: () => Either[Validation, SessionEvent], replayTo: ActorRef[Either[Validation, Session]]): Effect[SessionEvent, Session] =
        commandHandler() match {
            case Right(event) => Effect.persist(event).thenReply(replayTo)(state => Right(state))
            case Left(err) => Effect.unhandled.thenReply(replayTo)(_ => Left(err))
    }

    val commandHandler: (Session, Command) => Effect[SessionEvent, Session] = { (session, command) =>
        command match {
           case AddStory(playerId, story, replayTo) => handleEvent(() => session.addStory(playerId, story), replayTo)
           case StartGame(playerId, gameId, replayTo) => handleEvent(() => session.setGame(playerId, gameId), replayTo)
           case JoinPlayer(player) => Effect.persist(session.joinPlayer(player))
           case LeftPlayer(player) => Effect.persist(session.leftPlayer(player))
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
