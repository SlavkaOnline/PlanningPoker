package Domain

import java.util.UUID

object Position extends Enumeration {
    type Position = Value
    val Current, TurnedLeft, TurnedRight = Value
}

case class Card(id: UUID, name: String, column: Int, position: Position.Position) {

    def moveRight(): Either[Card, String] = {
        position match {
            case Position.TurnedRight => Right("The card can only be moved one step")
            case Position.Current => Left(copy(position = Position.TurnedRight, column = column + 1))
            case Position.TurnedLeft => Left(copy(position = Position.Current, column = column + 1))
        }
    }

    def moveLeft(): Either[Card, String] = {
        position match {
            case Position.TurnedLeft => Right("The card can only be moved one step")
            case Position.Current => Left(copy(position = Position.TurnedLeft, column = column - 1))
            case Position.TurnedRight => Left(copy(position = Position.Current, column = column - 1))
        }
    }
    def setCurrentPosition(): Card = copy(position = Position.Current)
}
