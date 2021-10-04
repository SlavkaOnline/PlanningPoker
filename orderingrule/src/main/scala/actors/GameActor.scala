package actors

import akka.actor.typed.scaladsl.Behaviors
import akka.actor.typed.{ActorRef, Behavior}
import domain.{Game, InvalidMoveDirection, MoveDirection, Validation}

import java.util.UUID

object GameActor {

    sealed trait Command
    final case class MoveCard(playerId: UUID, gameId: UUID, cardId: UUID, Direction: String, replayTo: ActorRef[Either[Validation, Game]]) extends Command
    final case class Next(playerId: UUID, replayTo: ActorRef[Either[Validation, Game]]) extends Command
    final case class JoinPlayer(playerId: UUID, replayTo:  ActorRef[Either[Validation, Game]]) extends Command
    final case class LeftPlayer(playerId: UUID, replayTo:  ActorRef[Either[Validation, Game]]) extends Command

    def apply(game: Game): Behavior[Command] = {
        Behaviors.receive { (context, cmd)  =>
            cmd match {
                case MoveCard(playerId, gameId, cardId, direction, replayTo) =>
                    val action = game.moveCard(playerId, gameId, cardId, _);
                    val result = direction.toLowerCase match {
                            case "left" => action(MoveDirection.Left)
                            case "right" => action(MoveDirection.Right)
                            case _ => Left(InvalidMoveDirection)
                        }
                    replayTo ! result
                    apply(game)

                case Next(playerId, replayTo) =>
                    replayTo ! game.next(playerId)
                    apply(game)
                case JoinPlayer(playerId, replayTo) =>
                    replayTo ! Right(game.joinPlayer(playerId))
                    apply(game)
                case LeftPlayer(playerId, replayTo) =>
                    replayTo ! Right(game.leftPlayer(playerId))
                    apply(game)

            }

        }
    }

}
